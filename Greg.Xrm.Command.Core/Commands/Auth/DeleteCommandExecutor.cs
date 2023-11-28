using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Auth
{
    public class DeleteCommandExecutor : ICommandExecutor<DeleteCommand>
	{
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IOutput output;

		public DeleteCommandExecutor(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output)
		{
			this.organizationServiceRepository = organizationServiceRepository;
			this.output = output;
		}

		public async Task ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Name))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "You must specify the name of the authentication profile to delete");
			}


			await organizationServiceRepository.DeleteConnectionAsync(command.Name);
			output.WriteLine($"Authentication profile '{command.Name}' deleted");
		}
	}
}
