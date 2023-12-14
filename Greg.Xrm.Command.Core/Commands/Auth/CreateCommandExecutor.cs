using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;

namespace Greg.Xrm.Command.Commands.Auth
{
    public class CreateCommandExecutor : ICommandExecutor<CreateCommand>
	{
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IOutput output;

		public CreateCommandExecutor(
			IOrganizationServiceRepository organizationServiceRepository, 
			IOutput output)
        {
			this.organizationServiceRepository = organizationServiceRepository;
			this.output = output;
		}

        public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Name))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "You must specify the name to be given to the authentication profile");
			}
			if (string.IsNullOrWhiteSpace(command.ConnectionString))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "You must specify the connectionString to be given to the authentication profile");
			}

			try
			{
				await organizationServiceRepository.SetConnectionAsync(command.Name, command.ConnectionString);
				return CommandResult.Success();
			}
			catch(DataverseConnectionException ex)
			{
				output.WriteLine(ex.Message, ConsoleColor.Red);
				if (ex.InnerException != null)
				{
					output.WriteLine("  " + ex.InnerException.Message, ConsoleColor.Red);
				}
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
