using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface ISolutionRepository
	{
		Task<Solution?> GetByUniqueNameAsync(IOrganizationServiceAsync2 crm, string uniqueName);
	}
}
