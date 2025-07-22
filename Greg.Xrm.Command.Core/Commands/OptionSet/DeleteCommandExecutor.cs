using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class DeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				if (!string.IsNullOrWhiteSpace(command.Name))
				{
					output.Write($"Updating option set '{command.Name}'...");
					publishXmlBuilder.AddGlobalOptionSet(command.Name);
				}
				else
				{
					output.Write($"Updating option set '{command.TableName}.{command.ColumnName}'...");
					publishXmlBuilder.AddTable(command.TableName);
				}


				var request = new DeleteOptionValueRequest
				{
					OptionSetName = command.Name,
					EntityLogicalName = command.TableName,
					AttributeLogicalName = command.ColumnName,
					Value = command.Value,
				};

				await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);


				output.Write($"Publishing changes...");
				await crm.ExecuteAsync(publishXmlBuilder.Build());
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
