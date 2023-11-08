using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class SetDefaultCommandExecutor : ICommandExecutor<SetDefaultCommand>
	{
		private readonly ILogger<SetDefaultCommandExecutor> logger;
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public SetDefaultCommandExecutor(
			ILogger<SetDefaultCommandExecutor> logger,
			IOutput output, 
			IOrganizationServiceRepository organizationServiceRepository)
        {
			this.logger = logger;
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task ExecuteAsync(SetDefaultCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.SolutionUniqueName))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The unique name of the solution to set as default is required.");

			var uniqueName = command.SolutionUniqueName;

			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			this.output.Write("Checking if solution '").Write(uniqueName).Write("' exists and is unmanaged...");
			try
			{
				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged", "uniquename");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
				query.TopCount = 1;
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				this.output.WriteLine("Done", ConsoleColor.Green);


				if (result.Entities.Count == 0)
				{
					this.output.WriteLine("Solution not found: " + command.SolutionUniqueName, ConsoleColor.Red);
					return;
				}

				var isManaged = result.Entities[0].GetAttributeValue<bool>("ismanaged");
				if (isManaged)
				{
					this.output.WriteLine("Cannot set a managed solution as default. Please peek an unmanaged solution", ConsoleColor.Red);
					return;
				}

				uniqueName = result.Entities[0].GetAttributeValue<string>("uniquename");
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine().WriteLine("Error while checking solution existence: " + ex.Message, ConsoleColor.Red);
				this.logger.LogError(ex, "Error while checking solution existence: {message}", ex.Message);
				return;
			}


			try
			{
				await this.organizationServiceRepository.SetDefaultSolutionAsync(uniqueName);
				this.output.Write("Solution '").Write(command.SolutionUniqueName, ConsoleColor.Yellow).WriteLine("' set as default.");
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("Error while setting default solution: " + ex.Message, ConsoleColor.Red);
				this.logger.LogError(ex, "Error while setting default solution: {message}", ex.Message);
			}
		}
	}
}
