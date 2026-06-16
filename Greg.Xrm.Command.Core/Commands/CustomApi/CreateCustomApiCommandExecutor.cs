using System.ServiceModel;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Model;
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
		ISolutionRepository solutionRepository)
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

				var uniqueName  = command.UniqueName!;
				var displayName = command.DisplayName ?? CustomApiDisplayNameHelper.InferDisplayName(uniqueName);

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

				// ── Create the customapi record ───────────────────────────────────────
				output.Write("Creating Custom API '");
				output.Write(uniqueName, ConsoleColor.Yellow);
				output.Write("'...");
				var api = new Model.CustomApi
				{
					uniquename   = uniqueName,
					displayname  = displayName,
					description  = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description,
					bindingtype  = new OptionSetValue((int)command.BindingType),
					isfunction   = command.Type == CustomApiType.Function,
					allowedcustomprocessingsteptype = new OptionSetValue((int)command.AllowedStepType),
					executeprivilegename = string.IsNullOrWhiteSpace(command.ExecutePrivilegeName) ? null : command.ExecutePrivilegeName
				};
				await api.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				await AddToSolutionAsync(crm, solutionName, ComponentType.CustomAPI, api.Id, cancellationToken);

				var createdParams    = new List<string>();
				var createdResponses = new List<string>();

				// ── Create parameters ─────────────────────────────────────────────────
				foreach (var paramSpec in CreateCustomApiCommand.SplitSpecs(command.Params))
				{
					CustomApiParamSpec.TryParse(paramSpec, out var spec, out _);
					var paramUniqueName  = $"{uniqueName}-in-{spec!.UniqueName}";
					var paramDisplayName = CustomApiDisplayNameHelper.InferDisplayName(spec.UniqueName);

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
						name         = paramUniqueName,
						uniquename   = paramUniqueName,
						displayname  = paramDisplayName,
						type         = new OptionSetValue(spec.TypeCode),
						isoptional   = spec.IsOptional,
						customapiid  = new EntityReference("customapi", api.Id)
					};
					await param.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);

					await AddToSolutionAsync(crm, solutionName, ComponentType.CustomAPIRequestParameter, param.Id, cancellationToken);
					createdParams.Add($"{paramUniqueName} ({spec.Type}{(spec.IsOptional ? ", optional" : "")})");
				}

				// ── Create response properties ────────────────────────────────────────
				foreach (var responseSpec in CreateCustomApiCommand.SplitSpecs(command.Responses))
				{
					CustomApiParamSpec.TryParse(responseSpec, out var spec, out _);
					var respUniqueName  = $"{uniqueName}-out-{spec!.UniqueName}";
					var respDisplayName = CustomApiDisplayNameHelper.InferDisplayName(spec.UniqueName);

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
							name        = respUniqueName,
							uniquename  = respUniqueName,
						displayname = respDisplayName,
						type        = new OptionSetValue(spec.TypeCode),
						customapiid = new EntityReference("customapi", api.Id)
					};
					await resp.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);

					await AddToSolutionAsync(crm, solutionName, ComponentType.CustomAPIResponseProperty, resp.Id, cancellationToken);
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
				result["Unique Name"]  = uniqueName;
				result["Solution"]     = solutionName;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static async Task AddToSolutionAsync(IOrganizationServiceAsync2 crm, string solutionName, ComponentType componentType, Guid componentId, CancellationToken cancellationToken)
		{
			var req = new AddSolutionComponentRequest
			{
				SolutionUniqueName       = solutionName,
				ComponentType            = (int)componentType,
				ComponentId              = componentId,
				AddRequiredComponents    = false,
				DoNotIncludeSubcomponents = true,
			};
			await crm.ExecuteAsync(req, cancellationToken);
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
