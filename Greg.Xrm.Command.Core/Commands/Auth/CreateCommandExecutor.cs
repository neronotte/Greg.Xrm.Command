using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;

namespace Greg.Xrm.Command.Commands.Auth
{
    public class CreateCommandExecutor(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output) : ICommandExecutor<CreateCommand>
	{
		private const string CONNECTION_STRING_TEMPLATE = "AuthType=OAuth;Url={0};RedirectUri=http://localhost;LoginPrompt=Auto";

        public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Name))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "You must specify the name to be given to the authentication profile");
			}


			var connectionString = command.ConnectionString;


			if (string.IsNullOrWhiteSpace(connectionString))
			{
				connectionString = CONNECTION_STRING_TEMPLATE.Replace("{0}", command.EnvironmentUrl?.TrimEnd('/'));
			}

			try
			{
				await organizationServiceRepository.SetConnectionAsync(command.Name, connectionString);
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
