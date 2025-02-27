
using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Views
{
	public class RenameCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		IViewRetrieverService viewRetriever,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<RenameCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RenameCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, view) = await viewRetriever.GetByNameAsync(crm, command.QueryType, command.OldName, command.TableName);
			if (view == null) return result;



			output.Write($"Renaming view '{view.name}' to '{command.NewName}'...");
			try
			{
				view.name = command.NewName;
				await view.SaveOrUpdateAsync(crm);

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while renaming the view: {ex.Message}", ex);
			}


			try
			{
				output.Write($"Publishing entity '{view.returnedtypecode}' ...");

				publishXmlBuilder.AddTable(view.returnedtypecode);
				var publishXml = publishXmlBuilder.Build();
				await crm.ExecuteAsync(publishXml);

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while publishing the entity: {ex.Message}", ex);
			}

			return CommandResult.Success();
		}
	}
}
