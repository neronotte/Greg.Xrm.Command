using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Views.Model
{
	public interface IViewRetrieverService {
		Task<(CommandResult, TableView?)> GetByNameAsync(
				IOrganizationServiceAsync2 crm,
				QueryType1 queryType,
				string viewName,
				string? tableName);
	}
}
