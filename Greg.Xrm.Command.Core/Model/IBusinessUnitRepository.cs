using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IBusinessUnitRepository
	{
		Task<IReadOnlyList<BusinessUnit>> GetAllAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken);
		Task<IReadOnlyList<BusinessUnit>> GetByIdsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> identifiers, CancellationToken cancellationToken);
		Task<BusinessUnit> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken);
	}
}