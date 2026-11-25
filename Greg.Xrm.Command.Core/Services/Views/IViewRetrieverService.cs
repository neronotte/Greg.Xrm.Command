using Greg.Xrm.Command.Commands.Views;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Views
{
	public interface IViewRetrieverService
	{
		Task<(CommandResult, TableView?)> GetByNameAsync(
				IOrganizationServiceAsync2 crm,
				QueryType1 queryType,
				string viewName,
				string? tableName);
	}
}
