using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
    public interface IWebResourceRepository
	{
		Task<List<WebResource>> GetByNameAsync(IOrganizationServiceAsync2 crm, string[] fileNames, bool fetchContent = false);
		Task<List<WebResource>> GetBySolutionAsync(IOrganizationServiceAsync2 crm, string solutionUniqueName, bool fetchContent = false);
	}
}
