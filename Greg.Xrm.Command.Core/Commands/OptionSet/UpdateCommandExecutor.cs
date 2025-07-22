using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class UpdateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<UpdateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(UpdateCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();

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


				var request = new UpdateOptionValueRequest
				{
					OptionSetName = command.Name,
					EntityLogicalName = command.TableName,
					AttributeLogicalName = command.ColumnName,
					Value = command.Value,
				};

				if (command.DisplayName is not null)
				{
					request.Label = new Microsoft.Xrm.Sdk.Label(command.DisplayName, defaultLanguageCode);
				}

				if (command.Color is not null)
				{
					request.Parameters["Color"] = command.Color;
				}

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
