using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Publish
{
	public class PublishAllCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository


	) : ICommandExecutor<PublishAllCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PublishAllCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write($"Publishing all customizations...");
			try
			{
				var request = new PublishAllXmlRequest();

				await crm.ExecuteAsync(request);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Error publishing customizations: {ex.Message}", ex);
			}

			return CommandResult.Success();
		}
	}
}
