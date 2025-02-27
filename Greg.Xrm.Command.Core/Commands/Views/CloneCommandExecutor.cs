using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Views
{
	public class CloneCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		IViewRetrieverService viewRetriever,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<CloneCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CloneCommand command, CancellationToken cancellationToken)
		{
			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}


			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var (result, view) = await viewRetriever.GetByNameAsync(crm, command.QueryType, command.OldName, command.TableName);
			if (view == null) return result;


			try
			{
				var newName = GetName(view.name, command.NewName);
				var message = command.Clean ? " removing all filters" : string.Empty;

				output.Write($"Cloning view '{view.name}' into '{newName}'{message}...");


				var newView = view.GetType() == typeof(SavedQuery) ? (TableView)new SavedQuery() : new UserQuery();
				newView.name = newName; 
				newView.layoutxml = view.layoutxml;
				newView.querytype = SavedQueryQueryType.MainApplicationView;
				newView.returnedtypecode = view.returnedtypecode;
				if (command.Clean)
				{
					newView.fetchxml = CleanFetchXml(view.fetchxml);
				}
				else
				{
					newView.fetchxml = view.fetchxml;
				}
				await newView.SaveOrUpdateAsync(crm);

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while creating the view: {ex.Message}", ex);
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



		public static string? CleanFetchXml(string? fetchxml)
		{
			if (string.IsNullOrWhiteSpace(fetchxml))
			{
				return fetchxml;
			}

			// parse the fetch xml and remove all the <filter> nodes and any subnodes
			var xml = XDocument.Parse(fetchxml);

			// Remove all <filter> elements
			xml.Descendants("filter").Remove();

			return xml.ToString();
		}






		private static string? GetName(string? name, string newName)
		{
			if (!string.IsNullOrWhiteSpace(newName))
			{
				return newName;
			}

			return name + " (new)";
		}
	}
}
