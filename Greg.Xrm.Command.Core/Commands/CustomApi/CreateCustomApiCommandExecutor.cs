using System.ServiceModel;
using System.Linq;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class CreateCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IObjectTypeCodeFinder objectTypeCodeFinder)
		: ICommandExecutor<CreateCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// ── Resolve solution ──────────────────────────────────────────────────
				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (string.IsNullOrWhiteSpace(solutionName))
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");

				output.Write("Validating solution '");
				output.Write(solutionName, ConsoleColor.Yellow);
				output.Write("'...");
				var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				if (solution == null)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Solution '{solutionName}' not found.");
				}
				if (solution.ismanaged)
				{
					output.WriteLine("Managed", ConsoleColor.Red);
					return CommandResult.Fail($"Solution '{solutionName}' is managed. Cannot add components to a managed solution.");
				}
				output.WriteLine("Done", ConsoleColor.Green);

				// ── Resolve publisher prefix from solution ────────────────────────────
				var publisherPrefix = solution.PublisherCustomizationPrefix;

				var displayName = command.DisplayName!;
				string uniqueName;
				if (!string.IsNullOrWhiteSpace(command.UniqueName))
				{
					// Validate the provided prefix matches the solution publisher
					var underscoreIdx = command.UniqueName.IndexOf('_');
					var providedPrefix = underscoreIdx > 0 ? command.UniqueName[..underscoreIdx] : command.UniqueName;
					if (publisherPrefix != null && !string.Equals(providedPrefix, publisherPrefix, StringComparison.OrdinalIgnoreCase))
						return CommandResult.Fail($"Unique name prefix '{providedPrefix}' does not match the solution publisher prefix '{publisherPrefix}'.");
					uniqueName = command.UniqueName;
				}
				else
				{
					if (string.IsNullOrWhiteSpace(publisherPrefix))
						return CommandResult.Fail("Cannot infer unique name: solution publisher customization prefix is not available.");
					uniqueName = CustomApiDisplayNameHelper.InferUniqueName(displayName, publisherPrefix);
				}

				// Idempotency check
				output.Write($"Checking if Custom API '");
				output.Write(uniqueName, ConsoleColor.Yellow);
				output.Write("' already exists...");
				var existing = await QueryCustomApiByNameAsync(crm, uniqueName);
				if (existing != null)
				{
					output.WriteLine("Already exists", ConsoleColor.Yellow);
					output.WriteLine($"Custom API '{uniqueName}' already exists. Skipping creation.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}
				output.WriteLine("Not found", ConsoleColor.Green);


				var (isValid, executePrivilegeName) = await TryValidateExecutePrivilegeNameAsync(crm, command.ExecutePrivilegeName);
				if (!isValid)
					return CommandResult.Fail($"Invalid execute privilege name '{command.ExecutePrivilegeName}'.");

				var paramSpecs = CreateCustomApiCommand.SplitSpecs(command.Params).ToList();
				var responseSpecs = CreateCustomApiCommand.SplitSpecs(command.Responses).ToList();
				var hasParams = paramSpecs.Count > 0;
				var hasResponses = responseSpecs.Count > 0;

				var customApiObjectTypeCode = await objectTypeCodeFinder.GetObjectTypeCodeForTableAsync(crm, "customapi", cancellationToken);
				var customApiRequestObjectTypeCode = hasParams
					? await objectTypeCodeFinder.GetObjectTypeCodeForTableAsync(crm, "customapirequestparameter", cancellationToken)
					: (int?)null;
				var customApiResponseObjectTypeCode = hasResponses
					? await objectTypeCodeFinder.GetObjectTypeCodeForTableAsync(crm, "customapiresponseproperty", cancellationToken)
					: (int?)null;

				// ── Create the customapi record ───────────────────────────────────────
				output.Write("Creating Custom API '");
				output.Write(uniqueName, ConsoleColor.Yellow);
				output.Write("'...");

				var description = command.Description;
				if (string.IsNullOrWhiteSpace(description))
				{
					description = uniqueName;
				}


				var api = new Model.CustomApi
				{
					name = displayName,
					uniquename = uniqueName,
					displayname = displayName,
					description = description,
					bindingtype = new OptionSetValue((int)command.BindingType),
					boundentitylogicalname = string.IsNullOrWhiteSpace(command.BoundEntityLogicalName) ? null : command.BoundEntityLogicalName,
					isfunction = command.Type == CustomApiType.Function,
					allowedcustomprocessingsteptype = new OptionSetValue((int)command.AllowedStepType),
					executeprivilegename = executePrivilegeName
				};

				await api.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				await AddToSolutionAsync(crm, output, solutionName, customApiObjectTypeCode, api.Id, cancellationToken);

				var createdParams = new List<string>();
				var createdResponses = new List<string>();

				// ── Create parameters ─────────────────────────────────────────────────
				foreach (var paramSpec in paramSpecs)
				{
					CustomApiParamSpec.TryParse(paramSpec, out var spec, out _);
					var paramUniqueName = spec!.UniqueName;  // already cleaned by TryParse

					output.Write("  Adding param '");
					output.Write(paramUniqueName, ConsoleColor.Yellow);
					output.Write("'...");

					var existingParam = await QueryParamByNameAndApiAsync(crm, paramUniqueName, api.Id);
					if (existingParam != null)
					{
						output.WriteLine("Already exists (skipped)", ConsoleColor.Yellow);
						continue;
					}

					var param = new CustomApiRequestParameter
					{
						name = CustomApiDisplayNameHelper.BuildParamName(displayName, paramUniqueName),
						uniquename = paramUniqueName,
						displayname = CustomApiDisplayNameHelper.BuildParamName(displayName, paramUniqueName),
						description = string.Empty,
						type = new OptionSetValue(spec.TypeCode),
						isoptional = spec.IsOptional,
						customapiid = new EntityReference("customapi", api.Id)
					};
					await param.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);

					await AddToSolutionAsync(crm, output, solutionName, customApiRequestObjectTypeCode!.Value, param.Id, cancellationToken);
					createdParams.Add($"{paramUniqueName} ({spec.Type}{(spec.IsOptional ? ", optional" : "")})");
				}

				// ── Create response properties ────────────────────────────────────────
				foreach (var responseSpec in responseSpecs)
				{
					CustomApiParamSpec.TryParse(responseSpec, out var spec, out _);
					var respUniqueName = spec!.UniqueName;  // already cleaned by TryParse

					output.Write("  Adding response '");
					output.Write(respUniqueName, ConsoleColor.Yellow);
					output.Write("'...");

					var existingResp = await QueryResponseByNameAndApiAsync(crm, respUniqueName, api.Id);
					if (existingResp != null)
					{
						output.WriteLine("Already exists (skipped)", ConsoleColor.Yellow);
						continue;
					}

					var resp = new CustomApiResponseProperty
					{
						name = CustomApiDisplayNameHelper.BuildResponseName(displayName, respUniqueName),
						uniquename = respUniqueName,
						displayname = CustomApiDisplayNameHelper.BuildResponseName(displayName, respUniqueName),
						description = string.Empty,
						type = new OptionSetValue(spec.TypeCode),
						customapiid = new EntityReference("customapi", api.Id)
					};
					await resp.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);

					await AddToSolutionAsync(crm, output, solutionName, customApiResponseObjectTypeCode!.Value, resp.Id, cancellationToken);
					createdResponses.Add($"{respUniqueName} ({spec.Type})");
				}

				output.WriteLine();
				output.Write("Custom API created: "); output.WriteLine(uniqueName, ConsoleColor.Green);
				output.Write("Display name:       "); output.WriteLine(displayName);
				output.Write("Solution:           "); output.WriteLine(solutionName, ConsoleColor.Cyan);
				if (createdParams.Count > 0)
					output.WriteLine($"Parameters:   {string.Join(", ", createdParams)}");
				if (createdResponses.Count > 0)
					output.WriteLine($"Responses:    {string.Join(", ", createdResponses)}");

				var result = CommandResult.Success();
				result["CustomApi Id"] = api.Id;
				result["Unique Name"] = uniqueName;
				result["Solution"] = solutionName;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private async Task<(bool, string?)> TryValidateExecutePrivilegeNameAsync(IOrganizationServiceAsync2 crm, string? input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return (true, null); // optional
			}

			var query = new QueryExpression("privilege") { NoLock = true, TopCount = 1 };
			query.ColumnSet.AddColumns("name");
			query.Criteria.AddCondition("name", ConditionOperator.Equal, input);

			var result = await crm.RetrieveMultipleAsync(query);
			if (result.Entities.Count == 1)
			{
				var foundName = result.Entities[0].GetAttributeValue<string>("name");
				return (true, foundName);
			}

			input = input.Trim().Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]"); // escape special characters for LIKE query

			query = new QueryExpression("privilege") { NoLock = true, TopCount = 2 };
			query.ColumnSet.AddColumns("name");
			query.Criteria.AddCondition("name", ConditionOperator.Like, '%' + input + '%');

			result = await crm.RetrieveMultipleAsync(query);
			if (result.Entities.Count == 0)
			{
				output.WriteLine("  Warning: No privilege found matching the provided execute privilege name.", ConsoleColor.Yellow);
				return (false, null);
			}

			if (result.Entities.Count > 1)
			{
				output.WriteLine(" Warning: Multiple privileges found matching the provided execute privilege name. Refine your privilege definition.");
				return (false, null);
			}

			var name = result.Entities[0].GetAttributeValue<string>("name");
			return (true, name);
		}

		private async Task AddToSolutionAsync(IOrganizationServiceAsync2 crm, IOutput output, string solutionName, int componentType, Guid componentId, CancellationToken cancellationToken)
		{
			try
			{
				var req = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solutionName,
					ComponentType = componentType,
					ComponentId = componentId
				};
				await crm.ExecuteAsync(req, cancellationToken);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine($"  Warning: could not add component to solution '{solutionName}': {ex.Message}", ConsoleColor.Yellow);
			}
		}

		private static async Task<Entity?> QueryCustomApiByNameAsync(IOrganizationServiceAsync2 crm, string uniqueName)
		{
			var q = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
			q.ColumnSet.AddColumn("customapiid");
			q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
			var result = await crm.RetrieveMultipleAsync(q);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private static async Task<Entity?> QueryParamByNameAndApiAsync(IOrganizationServiceAsync2 crm, string uniqueName, Guid apiId)
		{
			var q = new QueryExpression("customapirequestparameter") { NoLock = true, TopCount = 1 };
			q.ColumnSet.AddColumn("customapirequestparameterid");
			q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
			q.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
			var result = await crm.RetrieveMultipleAsync(q);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private static async Task<Entity?> QueryResponseByNameAndApiAsync(IOrganizationServiceAsync2 crm, string uniqueName, Guid apiId)
		{
			var q = new QueryExpression("customapiresponseproperty") { NoLock = true, TopCount = 1 };
			q.ColumnSet.AddColumn("customapiresponsepropertyid");
			q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
			q.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
			var result = await crm.RetrieveMultipleAsync(q);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}
	}
}
