using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Auth
{
	public class SelectCommandExecutor : ICommandExecutor<SelectCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository repository;

		public SelectCommandExecutor(IOutput output, IOrganizationServiceRepository repository)
        {
			this.output = output;
			this.repository = repository;
		}

        public async Task<CommandResult> ExecuteAsync(SelectCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Name))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The name of the authentication profile to set as default is required.");
			}

			var connections = await this.repository.GetAllConnectionDefinitionsAsync();

			if (!connections.Exists(command.Name))
			{
				return CommandResult.Fail("Invalid connection name: " + command.Name + Environment.NewLine + "use 'auth list' to see the list of available connections.");
			}

			if (connections.CurrentConnectionStringKey == command.Name)
			{
				this.output.Write("Connection '").Write(command.Name).WriteLine("' is already set as default.");
				return CommandResult.Success();
			}

			await this.repository.SetDefaultAsync(command.Name);
			this.output.Write("Connection '").Write(command.Name, ConsoleColor.Yellow).WriteLine("' set as default.");
			return CommandResult.Success();
		}
	}
}
