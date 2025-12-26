using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;

namespace Greg.Xrm.Command.Commands.Auth
{
    public class CreateCommandExecutor(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output) : ICommandExecutor<CreateCommand>
	{
		private const string OAUTH_CONNECTION_STRING_TEMPLATE = "AuthType=OAuth;Url={0};RedirectUri=http://localhost;LoginPrompt=Auto";
		private const string CLIENTID_CONNECTION_STRING_TEMPLATE = "AuthType=ClientSecret;Url={0};ClientId={1};ClientSecret={2}";

		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Name))
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "You must specify the name to be given to the authentication profile");
			}


			var connectionString = command.ConnectionString;


			if (!connectionString.HasData())
			{
				if (!command.ApplicationId.HasData())
				{
					connectionString = OAUTH_CONNECTION_STRING_TEMPLATE.Replace("{0}", command.EnvironmentUrl?.TrimEnd('/'));
				}
				else
				{
					connectionString = CLIENTID_CONNECTION_STRING_TEMPLATE
						.Replace("{0}", command.EnvironmentUrl?.TrimEnd('/')?.Trim())
						.Replace("{1}", command.ApplicationId?.Trim('{', ' ', '}'))
						.Replace("{2}", command.ClientSecret?.Trim());
				}
			}

			try
			{
				output.Write("Creating authentication profile...");
				await organizationServiceRepository.SetConnectionAsync(command.Name, connectionString!);
				output.WriteLine("Done.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch(DataverseConnectionException ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
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
