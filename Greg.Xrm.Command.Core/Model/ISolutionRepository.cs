using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public interface ISolutionRepository
	{
		Task<Solution?> GetByUniqueNameAsync(IOrganizationServiceAsync2 crm, string uniqueName);

		Task<ITemporarySolution> CreateTemporarySolutionAsync(IOrganizationServiceAsync2 crm, EntityReference publisherRef);
	}
}
