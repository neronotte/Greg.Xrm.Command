using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	public class ReplicateCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		IViewRetrieverService viewRetriever,
		ISavedQueryRepository savedQueryRepository,
		IPublishXmlBuilder publishXmlBuilder
	) : ICommandExecutor<ReplicateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ReplicateCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			var (result, view) = await viewRetriever.GetByNameAsync(crm, QueryType1.SavedQuery, command.ViewName, command.TableName);
			if (view == null) return result;

			output.Write($"Retrieving other views from table '{view.returnedtypecode}'...");
			var otherViews = await savedQueryRepository.GetByTableNameAsync(crm, view.returnedtypecode);
			output.WriteLine("Done", ConsoleColor.Green);


			if (string.IsNullOrWhiteSpace(command.Onto))
			{
				otherViews = otherViews
					.Where(o => o.Id != view.Id)
					.Where(o => o.querytype != SavedQueryQueryType.LookupView)
					.ToList();
			}
			else if (command.Onto.Trim() == "*") 
			{
				otherViews = otherViews
					.Where(o => o.Id != view.Id)
					.ToList();

			} else {
				
				var names = command.Onto.Split(",", StringSplitOptions.RemoveEmptyEntries)
					.Select(n => n.Trim())
					.ToList();

				otherViews = otherViews
					.Where(o => o.Id != view.Id)
					.Where(o => names.Contains(o.name, StringComparer.OrdinalIgnoreCase))
					.ToList();
			}

			if (otherViews.Count == 0)
			{
				return CommandResult.Fail($"No views found for table '{view.returnedtypecode}' except for the source view");
			}


			try
			{
				output.Write("Replicating layout, please wait...");

				var operationResult = await Replicator.PropagateLayoutAsync(crm, (SavedQuery)view, otherViews, true, true, true);


				if (operationResult.Count > 0)
				{
					output.WriteLine("Warning", ConsoleColor.Yellow);

					output.WriteLine();
					output.WriteTable(operationResult, () => ["View", "Error"], x => [x.Item1, x.Item2]);
				}
				else
				{
					output.WriteLine("Done", ConsoleColor.Green);
				}

				if (operationResult.Count == otherViews.Count)
				{
					return CommandResult.Fail($"Unable to replicate the selected view layout");
				}
			}
			catch(Exception ex)
			{
				output.WriteLine("Error", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while replicating the layout: {ex.Message}", ex);
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
