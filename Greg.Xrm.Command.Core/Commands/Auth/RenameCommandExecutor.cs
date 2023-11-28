using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Auth
{
    public class RenameCommandExecutor : ICommandExecutor<RenameCommand>
	{
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IOutput output;

		public RenameCommandExecutor(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output)
		{
			this.organizationServiceRepository = organizationServiceRepository;
			this.output = output;
		}
		public async Task ExecuteAsync(RenameCommand command, CancellationToken cancellationToken)
		{
			await organizationServiceRepository.RenameConnectionAsync(command.OldName, command.NewName);

			this.output.WriteLine($"Authentication profile '{command.OldName}' renamed in '{command.NewName}'. Type 'pacx auth list' to check the updated name.");
		}
	}
}
