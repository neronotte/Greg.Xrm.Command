
using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Views
{
	public class DeleteCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		IViewRetrieverService viewRetriever,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, view) = await viewRetriever.GetByNameAsync(crm, command.QueryType, command.ViewName, command.TableName);
			if (view == null) return result;

			try
			{
				output.Write($"Deleting view {view.name} of table {view.returnedtypecode}...");
				await view.DeleteAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
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
