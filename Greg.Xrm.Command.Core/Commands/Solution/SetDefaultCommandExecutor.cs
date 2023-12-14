using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
    public class SetDefaultCommandExecutor : ICommandExecutor<SetDefaultCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public SetDefaultCommandExecutor(
			IOutput output, 
			IOrganizationServiceRepository organizationServiceRepository)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task<CommandResult> ExecuteAsync(SetDefaultCommand command, CancellationToken cancellationToken)
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
					return CommandResult.Fail("Solution not found: " + command.SolutionUniqueName);
				}

				var isManaged = result.Entities[0].GetAttributeValue<bool>("ismanaged");
				if (isManaged)
				{
					return CommandResult.Fail("Cannot set a managed solution as default. Please peek an unmanaged solution");
				}

				uniqueName = result.Entities[0].GetAttributeValue<string>("uniquename");
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail("Error while checking solution existence: " + ex.Message, ex);
			}


			try
			{
				await this.organizationServiceRepository.SetDefaultSolutionAsync(uniqueName);
				this.output.Write("Solution '").Write(command.SolutionUniqueName, ConsoleColor.Yellow).WriteLine("' set as default.");
				return CommandResult.Success();
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail("Error while setting default solution: " + ex.Message, ex);
			}
		}
	}
}
