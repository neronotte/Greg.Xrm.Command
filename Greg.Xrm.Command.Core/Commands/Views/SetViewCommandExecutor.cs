using System.Xml;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Views;

namespace Greg.Xrm.Command.Commands.Views
{
	public abstract class SetViewCommandExecutor<TCommand>(
		IOrganizationServiceRepository connection, IOutput output, IViewRetrieverService retriever,
		IPublishXmlBuilder publisher, IViewXmlEditor xmlEditor) : ICommandExecutor<TCommand>
		where TCommand : SetViewCommand
	{
		protected IViewXmlEditor XmlEditor { get; } = xmlEditor;

		protected abstract (string FetchXml, string? LayoutXml, IReadOnlyList<string> UnusedAttributes) Edit(TableView view, TCommand command);

		public async Task<CommandResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connection.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				var (result, view) = await retriever.GetByNameAsync(crm, command.QueryType, command.ViewName, command.TableName);
				if (view == null) return result;

				(string FetchXml, string? LayoutXml, IReadOnlyList<string> UnusedAttributes) updated;
				try
				{
					updated = Edit(view, command);
				}
				catch (Exception ex) when (ex is ArgumentException or XmlException or InvalidOperationException)
				{
					return CommandResult.Fail($"Invalid view definition: {ex.Message}", ex);
				}

				try
				{
					output.Write("Updating view '").Write(view.name, ConsoleColor.Yellow).Write("'...");
					view.fetchxml = updated.FetchXml;
					if (updated.LayoutXml != null) view.layoutxml = updated.LayoutXml;
					await view.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("Error", ConsoleColor.Red);
					return CommandResult.Fail($"An error occurred while updating the view: {ex.Message}", ex);
				}
				if (updated.UnusedAttributes.Count > 0)
					output.WriteLine("Warning: FetchXML selects attributes not shown in the layout: " +
						string.Join(", ", updated.UnusedAttributes) + ". Removing them may make the query more efficient.", ConsoleColor.Yellow);
				var success = CommandResult.Success();
				success["ViewId"] = view.Id;
				success["ViewName"] = view.name ?? command.ViewName;
				success["TableName"] = view.returnedtypecode;
				success["Published"] = false;
				if (!command.Publish) return success;

				try
				{
					output.Write("Publishing entity '").Write(view.returnedtypecode, ConsoleColor.Yellow).Write("'...");
					publisher.AddTable(view.returnedtypecode);
					await crm.ExecuteAsync(publisher.Build(), cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("Error", ConsoleColor.Red);
					return CommandResult.Fail($"An error occurred while publishing the entity: {ex.Message}", ex);
				}

				success["Published"] = true;
				return success;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to update view '{command.ViewName}': {ex.Message}", ex);
			}
		}
	}
}
