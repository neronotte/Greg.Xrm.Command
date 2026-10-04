using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IPrivilegeRepository
	{
		Task<IReadOnlyList<Privilege>> GetAllAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken);
		Task<Privilege?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken);
	}
}