using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Views.Model
{
	public class ViewRetrieverService(
		IOutput output,
		ISavedQueryRepository savedQueryRepository,
		IUserQueryRepository userQueryRepository
	) : IViewRetrieverService
	{
        public async Task<(CommandResult, TableView?)> GetByNameAsync(
			IOrganizationServiceAsync2 crm, 
			QueryType1 queryType,
			string viewName,
			string? tableName)
		{

			IReadOnlyList<TableView> viewList = [];
			if (queryType == QueryType1.SavedQuery)
			{

				output.Write($"Retrieving saved query '{viewName}'...");

				var savedQueries = await savedQueryRepository.GetByNameAsync(crm, viewName);
				viewList = savedQueries;

				output.WriteLine("Done", ConsoleColor.Green);
			}
			if (queryType == QueryType1.UserQuery)
			{
				output.Write($"Retrieving user query '{viewName}'...");
				var userQueries = await userQueryRepository.GetByNameAsync(crm, viewName);
				viewList = userQueries;
				output.WriteLine("Done", ConsoleColor.Green);
			}

			if (viewList.Count == 0)
			{
				return (CommandResult.Fail($"Unable to find a view called '{viewName}'."), null);
			}

			if (viewList.Count > 1)
			{
				if (string.IsNullOrWhiteSpace(tableName))
				{
					return (CommandResult.Fail($"Found {viewList.Count} views called '{viewName}'. Please specify the table name."), null);
				}

				viewList = viewList
					.Where(x => string.Equals(x.returnedtypecode, tableName, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (viewList.Count == 0)
				{
					return (CommandResult.Fail($"Unable to find a view called '{viewName}' for table '{tableName}'."), null);
				}

				if (viewList.Count > 1)
				{
					return (CommandResult.Fail($"Found {viewList.Count} views called '{viewName}' for table '{tableName}'. Cannot determine which one to update."), null);
				}
			}

			var view = viewList[0];



			if (tableName != null && !string.Equals(tableName, view.returnedtypecode, StringComparison.OrdinalIgnoreCase))
			{
				return (CommandResult.Fail($"The view '{viewName}' is not associated with the table '{tableName}'."), null);
			}


			return (CommandResult.Success(), view);
		}
    }
}
