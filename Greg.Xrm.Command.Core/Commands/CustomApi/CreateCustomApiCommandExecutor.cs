using System.ServiceModel;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class CreateCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<CreateCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var uniqueName = command.UniqueName!;
				var displayName = command.DisplayName ?? CustomApiDisplayNameHelper.InferDisplayName(uniqueName);

				// Idempotency check
				output.Write($"Checking if Custom API '{uniqueName}' already exists...");
				var existing = await QueryCustomApiByNameAsync(crm, uniqueName);
				if (existing != null)
				{
					output.WriteLine("Already exists", ConsoleColor.Yellow);
					output.WriteLine($"Custom API '{uniqueName}' already exists. Skipping creation.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}
				output.WriteLine("Not found", ConsoleColor.Green);

				// Create the customapi record
				output.Write($"Creating Custom API '{uniqueName}'...");
				var api = new Model.CustomApi
				{
					uniquename = uniqueName,
					displayname = displayName,
					description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description,
					bindingtype = new OptionSetValue((int)command.BindingType),
					isfunction = command.Type == CustomApiType.Function,
					allowedcustomprocessingsteptype = new OptionSetValue((int)command.AllowedStepType),
					executeprivilegename = string.IsNullOrWhiteSpace(command.ExecutePrivilegeName) ? null : command.ExecutePrivilegeName
				};

				await api.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				var createdParams = new List<string>();
				var createdResponses = new List<string>();

				// Create parameters
				foreach (var paramSpec in CreateCustomApiCommand.SplitSpecs(command.Params))
				{
					CustomApiParamSpec.TryParse(paramSpec, out var spec, out _);
					var paramUniqueName = $"{uniqueName}-in-{spec!.UniqueName}";
					var paramDisplayName = CustomApiDisplayNameHelper.InferDisplayName(spec.UniqueName);

					output.Write($"  Adding param '{paramUniqueName}'...");

					var existingParam = await QueryParamByNameAndApiAsync(crm, paramUniqueName, api.Id);
					if (existingParam != null)
					{
						output.WriteLine("Already exists (skipped)", ConsoleColor.Yellow);
						continue;
					}

					var param = new CustomApiRequestParameter
					{
						uniquename = paramUniqueName,
						displayname = paramDisplayName,
						type = new OptionSetValue(spec.TypeCode),
						isoptional = spec.IsOptional,
						customapiid = new EntityReference("customapi", api.Id)
					};
					await param.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);
					createdParams.Add($"{paramUniqueName} ({spec.Type}{(spec.IsOptional ? ", optional" : "")})");
				}

				// Create response properties
				foreach (var responseSpec in CreateCustomApiCommand.SplitSpecs(command.Responses))
				{
					CustomApiParamSpec.TryParse(responseSpec, out var spec, out _);
					var respUniqueName = $"{uniqueName}-out-{spec!.UniqueName}";
					var respDisplayName = CustomApiDisplayNameHelper.InferDisplayName(spec.UniqueName);

					output.Write($"  Adding response '{respUniqueName}'...");

					var existingResp = await QueryResponseByNameAndApiAsync(crm, respUniqueName, api.Id);
					if (existingResp != null)
					{
						output.WriteLine("Already exists (skipped)", ConsoleColor.Yellow);
						continue;
					}

					var resp = new CustomApiResponseProperty
					{
						uniquename = respUniqueName,
						displayname = respDisplayName,
						type = new OptionSetValue(spec.TypeCode),
						customapiid = new EntityReference("customapi", api.Id)
					};
					await resp.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);
					createdResponses.Add($"{respUniqueName} ({spec.Type})");
				}

				output.WriteLine();
				output.WriteLine($"Custom API created: {uniqueName}", ConsoleColor.Green);
				output.WriteLine($"Display name: {displayName}");
				if (createdParams.Count > 0)
					output.WriteLine($"Parameters:   {string.Join(", ", createdParams)}");
				if (createdResponses.Count > 0)
					output.WriteLine($"Responses:    {string.Join(", ", createdResponses)}");

				var result = CommandResult.Success();
				result["CustomApi Id"] = api.Id;
				result["Unique Name"] = uniqueName;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static async Task<Entity?> QueryCustomApiByNameAsync(Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 crm, string uniqueName)
		{
			var q = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
			q.ColumnSet.AddColumn("customapiid");
			q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
			var result = await crm.RetrieveMultipleAsync(q);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private static async Task<Entity?> QueryParamByNameAndApiAsync(Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 crm, string uniqueName, Guid apiId)
		{
			var q = new QueryExpression("customapirequestparameter") { NoLock = true, TopCount = 1 };
			q.ColumnSet.AddColumn("customapirequestparameterid");
			q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
			q.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
			var result = await crm.RetrieveMultipleAsync(q);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private static async Task<Entity?> QueryResponseByNameAndApiAsync(Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 crm, string uniqueName, Guid apiId)
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
