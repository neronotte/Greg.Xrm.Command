using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface ISolutionComponentRepository
	{
		Task<List<SolutionComponent>> GetBySolutionIdAsync(IOrganizationServiceAsync2 crm, Guid solutionId);
	}
}
