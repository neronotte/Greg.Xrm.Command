using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class DeleteCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<DeleteCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API with param/response counts
				output.Write($"Resolving Custom API '{command.UniqueName}'...");
				var apiQ = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				apiQ.ColumnSet.AddColumns("customapiid", "uniquename", "displayname");
				apiQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.UniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(apiQ);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.UniqueName}' not found.");
				}
				var apiId = apiResult.Entities[0].Id;
				var displayName = apiResult.Entities[0].GetAttributeValue<string>("displayname") ?? command.UniqueName;
				output.WriteLine("Done", ConsoleColor.Green);

				if (!command.Force)
				{
					// Count children for confirmation prompt
					var paramCountQ = new QueryExpression("customapirequestparameter") { NoLock = true };
					paramCountQ.ColumnSet.AllColumns = false;
					paramCountQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
					var paramCount = (await crm.RetrieveMultipleAsync(paramCountQ)).Entities.Count;

					var respCountQ = new QueryExpression("customapiresponseproperty") { NoLock = true };
					respCountQ.ColumnSet.AllColumns = false;
					respCountQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
					var respCount = (await crm.RetrieveMultipleAsync(respCountQ)).Entities.Count;

					output.WriteLine();
					output.WriteLine($"Custom API:  {command.UniqueName} ({displayName})", ConsoleColor.Yellow);
					output.WriteLine($"Parameters:  {paramCount}", ConsoleColor.Yellow);
					output.WriteLine($"Responses:   {respCount}", ConsoleColor.Yellow);
					output.WriteLine();
					output.Write("Are you sure you want to delete this Custom API and all its children? (y/N) ");

					var confirmation = Console.ReadLine()?.Trim();
					if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
					{
						output.WriteLine("Aborted.", ConsoleColor.Yellow);
						return CommandResult.Success();
					}
				}

				output.Write($"Deleting Custom API '{command.UniqueName}'...");
				// Dataverse cascades the delete to all related parameters and response properties
				await crm.DeleteAsync("customapi", apiId);
				output.WriteLine("Done", ConsoleColor.Green);
				output.WriteLine($"Custom API '{command.UniqueName}' and all its children have been deleted.", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
