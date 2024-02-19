using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface ISavedQueryRepository
	{
		Task<IReadOnlyList<SavedQuery>> GetByIdAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> ids);
		Task<IReadOnlyList<SavedQuery>> GetContainingAsync(IOrganizationServiceAsync2 crm, string tableName, string columnName);
	}
}
