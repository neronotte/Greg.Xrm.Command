using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class DeleteCommandExecutor : ICommandExecutor<DeleteCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public DeleteCommandExecutor(
			IOutput output, 
			IOrganizationServiceRepository organizationServiceRepository)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


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
					output.WriteLine($"Solution {command.SolutionUniqueName} not found", ConsoleColor.Red);
				}


				output.Write("Deleting solution ").Write(command.SolutionUniqueName, ConsoleColor.Yellow).Write("...");

				var solutionRef = result.Entities[0].ToEntityReference();

				await crm.DeleteAsync(solutionRef.LogicalName, solutionRef.Id, cancellationToken);

				output.WriteLine(" Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
		}
	}
}
