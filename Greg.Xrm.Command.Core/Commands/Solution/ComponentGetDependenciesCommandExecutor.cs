using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
    public class ComponentGetDependenciesCommandExecutor : ICommandExecutor<ComponentGetDependenciesCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IDependencyRepository dependencyRepository;

		public ComponentGetDependenciesCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IDependencyRepository dependencyRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.dependencyRepository = dependencyRepository;
		}


		public async Task<CommandResult> ExecuteAsync(ComponentGetDependenciesCommand command, CancellationToken cancellationToken)
		{
			if (command.ComponentId == Guid.Empty)
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The component id is required");
			}
			if (command.ComponentType == null)
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The component type is required");
			}


			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var dependencies = await this.dependencyRepository.GetDependenciesAsync(crm, command.ComponentType.Value, command.ComponentId);
				if (dependencies.Count == 0)
				{
					this.output.WriteLine("No dependencies found!", ConsoleColor.Cyan);
					return CommandResult.Success();
				}

				dependencies.WriteTo(this.output);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
