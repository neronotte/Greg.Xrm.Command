using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class RemoveCustomApiParamCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<RemoveCustomApiParamCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RemoveCustomApiParamCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API
				output.Write($"Resolving Custom API '{command.ApiUniqueName}'...");
				var apiQ = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				apiQ.ColumnSet.AddColumn("customapiid");
				apiQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.ApiUniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(apiQ);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.ApiUniqueName}' not found.");
				}
				var apiId = apiResult.Entities[0].Id;
				output.WriteLine("Done", ConsoleColor.Green);

				// Resolve the parameter
					var paramUniqueName = $"{command.ApiUniqueName}-in-{command.ParamUniqueName}";
					output.Write($"Resolving parameter '{paramUniqueName}'...");
					var paramQ = new QueryExpression("customapirequestparameter") { NoLock = true, TopCount = 1 };
					paramQ.ColumnSet.AddColumn("customapirequestparameterid");
					paramQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, paramUniqueName);
					paramQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
					var paramResult = await crm.RetrieveMultipleAsync(paramQ);
					if (paramResult.Entities.Count == 0)
					{
						output.WriteLine("Not found", ConsoleColor.Red);
						return CommandResult.Fail($"Parameter '{paramUniqueName}' not found on Custom API '{command.ApiUniqueName}'.");
					}
					var paramId = paramResult.Entities[0].Id;
					output.WriteLine("Done", ConsoleColor.Green);

					output.Write($"Deleting parameter '{paramUniqueName}'...");
				await crm.DeleteAsync("customapirequestparameter", paramId);
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
