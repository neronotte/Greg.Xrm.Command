using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Key
{
	public class DeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	): ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.Write($"Deleting key '{command.SchemaName}' on table '{command.Table}'...");
				var request = new DeleteEntityKeyRequest
				{
					EntityLogicalName = command.Table,
					Name = command.SchemaName
				};
				await crm.ExecuteAsync(request);
				
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error: " + ex.Message);
			}
		}
	}

}
