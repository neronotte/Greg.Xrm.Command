using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
    public interface IWebResourceRepository
	{
		Task<List<WebResource>> GetBySolutionAsync(IOrganizationServiceAsync2 crm, string solutionUniqueName, bool fetchContent = false);
	}
}
