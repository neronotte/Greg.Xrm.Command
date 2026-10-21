using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IRibbonComponentRepository
	{
		Task<IReadOnlyList<RibbonComponent>> GetSolutionComponentsAsync(IOrganizationServiceAsync2 crm, Guid solutionId, int maximumCount, CancellationToken cancellationToken);
		Task<IReadOnlyList<Guid>> GetApplicationRibbonIdsAsync(IOrganizationServiceAsync2 crm, bool activeOnly, Guid? ribbonId, CancellationToken cancellationToken);
		Task<(string? TableName, string? Name)> GetComponentNamesAsync(IOrganizationServiceAsync2 crm, ComponentType componentType, Guid objectId, CancellationToken cancellationToken);
	}
}
