using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class DeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumn("uniquename");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.SolutionUniqueName);
				query.TopCount = 1;
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					output.WriteLine($"Solution {command.SolutionUniqueName} not found.", ConsoleColor.Red);
					return CommandResult.Fail($"Solution '{command.SolutionUniqueName}' not found.");
				}


				output.Write("Deleting solution ").Write(command.SolutionUniqueName, ConsoleColor.Yellow).Write("...");

				var solutionRef = result.Entities[0].ToEntityReference();

				await crm.DeleteAsync(solutionRef.LogicalName, solutionRef.Id, cancellationToken);

				output.WriteLine(" Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
