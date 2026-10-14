using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IOrganizationRepository
	{
		Task<Organization> GetAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken);
	}
}