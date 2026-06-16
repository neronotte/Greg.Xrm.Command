using System.ServiceModel;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class AddCustomApiParamCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<AddCustomApiParamCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddCustomApiParamCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API
				output.Write($"Resolving Custom API '{command.ApiUniqueName}'...");
				var q = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				q.ColumnSet.AddColumn("customapiid");
				q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.ApiUniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(q);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.ApiUniqueName}' not found.");
				}
				var apiId = apiResult.Entities[0].Id;
				output.WriteLine("Done", ConsoleColor.Green);




				CustomApiParamSpec.TryParse(command.Param!, out var spec, out _);
				var paramUniqueName = $"{command.ApiUniqueName}-in-{spec!.UniqueName}";
				var displayName = command.DisplayName ?? CustomApiDisplayNameHelper.InferDisplayName(spec.UniqueName);



				// Idempotency check
				var existingQ = new QueryExpression("customapirequestparameter") { NoLock = true, TopCount = 1 };
				existingQ.ColumnSet.AddColumn("customapirequestparameterid");
				existingQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, paramUniqueName);
				existingQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
				var existingResult = await crm.RetrieveMultipleAsync(existingQ);
				if (existingResult.Entities.Count > 0)
					return CommandResult.Fail($"Parameter '{paramUniqueName}' already exists on Custom API '{command.ApiUniqueName}'.");


				output.Write($"Adding parameter '{paramUniqueName}'...");
				var param = new CustomApiRequestParameter
				{
						name        = spec.UniqueName,
						uniquename  = paramUniqueName,
					displayname = displayName,
					description = command.Description ?? string.Empty,
					type = new OptionSetValue(spec.TypeCode),
					isoptional = spec.IsOptional,
					customapiid = new EntityReference("customapi", apiId)
				};
				await param.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Parameter Id"] = param.Id;
				result["Parameter Name"] = paramUniqueName;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
