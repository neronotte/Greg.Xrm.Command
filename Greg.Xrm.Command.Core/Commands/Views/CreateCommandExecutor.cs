using System.Xml;
using Greg.Xrm.Command.Commands.Views.Model;
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
		IPublishXmlBuilder publisher) : ICommandExecutor<CreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			string tableName;
			try
			{
				tableName = ViewXmlEditor.GetTableName(command.FetchXml);
			}
			catch (Exception ex) when (ex is ArgumentException or XmlException)
			{
				return CommandResult.Fail($"Invalid FetchXML: {ex.Message}", ex);
			}

			var crm = await connection.GetCurrentConnectionAsync();

			EntityMetadata metadata;
			try
			{
				output.Write($"Retrieving metadata for table '{tableName}'...");
				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
				{
					LogicalName = tableName,
					EntityFilters = EntityFilters.Entity
				}, cancellationToken);
				metadata = response.EntityMetadata;
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to retrieve metadata for table '{tableName}': {ex.Message}", ex);
			}

			(string FetchXml, string LayoutXml) definition;
			try
			{
				definition = ViewXmlEditor.CreateFromFetchXml(command.FetchXml, metadata);
			}
			catch (Exception ex) when (ex is ArgumentException or XmlException or InvalidOperationException)
			{
				return CommandResult.Fail($"Invalid view definition: {ex.Message}", ex);
			}

			try
			{
				output.Write($"Creating view '{command.ViewName}'...");
				TableView view = command.QueryType == QueryType1.SavedQuery ? new SavedQuery() : new UserQuery();
				view.name = command.ViewName;
				view.returnedtypecode = tableName;
				view.querytype = SavedQueryQueryType.MainApplicationView;
				view.fetchxml = definition.FetchXml;
				view.layoutxml = definition.LayoutXml;
				await view.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to create view '{command.ViewName}': {ex.Message}", ex);
			}

			if (!command.Publish) return CommandResult.Success();

			try
			{
				output.Write($"Publishing entity '{tableName}' ...");
				publisher.AddTable(tableName);
				await crm.ExecuteAsync(publisher.Build(), cancellationToken);
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
