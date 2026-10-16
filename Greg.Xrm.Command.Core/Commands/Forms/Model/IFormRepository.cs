using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Forms.Model
{
	public interface IFormRepository
	{
		Task<List<Form>> GetMainFormByTableNameAsync(IOrganizationServiceAsync2 crm, string tableName);

		Task<(CommandResult, Form?)> GetByNameAsync(IOrganizationServiceAsync2 crm, string tableName, string? formName, bool announce = true);
	}
}
