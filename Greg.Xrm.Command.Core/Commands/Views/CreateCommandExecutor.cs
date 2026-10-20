using Greg.Xrm.Command.Services.Views;
using System.Xml;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Views
{
	public class CreateCommandExecutor(
		IOrganizationServiceRepository connection,
		IOutput output,
		IPublishXmlBuilder publisher,
		IViewXmlEditor xmlEditor) : ICommandExecutor<CreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			string tableName;
			try
			{
				tableName = xmlEditor.GetTableName(command.FetchXml);
			}
			catch (Exception ex) when (ex is ArgumentException or XmlException)
			{
				return CommandResult.Fail($"Invalid FetchXML: {ex.Message}", ex);
			}

			try
			{
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connection.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				EntityMetadata metadata;
				try
				{
					output.Write("Retrieving metadata for table '").Write(tableName, ConsoleColor.Yellow).Write("'...");
					var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
					{
						LogicalName = tableName,
						EntityFilters = EntityFilters.Entity
					}, cancellationToken);
					metadata = response.EntityMetadata;
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("Error", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to retrieve metadata for table '{tableName}': {ex.Message}", ex);
				}

				(string FetchXml, string LayoutXml) definition;
				try
				{
					definition = xmlEditor.CreateFromFetchXml(command.FetchXml, metadata);
				}
				catch (Exception ex) when (ex is ArgumentException or XmlException or InvalidOperationException)
				{
					return CommandResult.Fail($"Invalid view definition: {ex.Message}", ex);
				}

				TableView view;
				try
				{
					output.Write("Creating view '").Write(command.ViewName, ConsoleColor.Yellow).Write("'...");
					view = command.QueryType == QueryType1.SavedQuery ? new SavedQuery() : new UserQuery();
					view.name = command.ViewName;
					view.returnedtypecode = tableName;
					view.querytype = SavedQueryQueryType.MainApplicationView;
					view.fetchxml = definition.FetchXml;
					view.layoutxml = definition.LayoutXml;
					await view.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("Error", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to create view '{command.ViewName}': {ex.Message}", ex);
				}

				var success = CommandResult.Success();
				success["ViewId"] = view.Id;
				success["ViewName"] = command.ViewName;
				success["TableName"] = tableName;
				success["Published"] = false;
				if (!command.Publish) return success;

				try
				{
					output.Write("Publishing entity '").Write(tableName, ConsoleColor.Yellow).Write("'...");
					publisher.AddTable(tableName);
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
				return CommandResult.Fail($"Unable to create view '{command.ViewName}': {ex.Message}", ex);
			}
		}
	}
}
