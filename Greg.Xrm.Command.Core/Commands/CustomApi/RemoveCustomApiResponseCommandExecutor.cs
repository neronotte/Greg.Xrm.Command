using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class RemoveCustomApiResponseCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<RemoveCustomApiResponseCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RemoveCustomApiResponseCommand command, CancellationToken cancellationToken)
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

				// Resolve the response property
					var respUniqueName = $"{command.ApiUniqueName}-out-{command.ResponseUniqueName}";
					output.Write($"Resolving response property '{respUniqueName}'...");
					var respQ = new QueryExpression("customapiresponseproperty") { NoLock = true, TopCount = 1 };
					respQ.ColumnSet.AddColumn("customapiresponsepropertyid");
					respQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, respUniqueName);
					respQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
					var respResult = await crm.RetrieveMultipleAsync(respQ);
					if (respResult.Entities.Count == 0)
					{
						output.WriteLine("Not found", ConsoleColor.Red);
						return CommandResult.Fail($"Response property '{respUniqueName}' not found on Custom API '{command.ApiUniqueName}'.");
					}
					var respId = respResult.Entities[0].Id;
					output.WriteLine("Done", ConsoleColor.Green);

					output.Write($"Deleting response property '{respUniqueName}'...");
				await crm.DeleteAsync("customapiresponseproperty", respId);
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
