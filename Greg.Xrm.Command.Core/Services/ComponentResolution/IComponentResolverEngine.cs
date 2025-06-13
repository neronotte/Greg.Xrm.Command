using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public interface IComponentResolverEngine
	{
		Task ResolveAllAsync(IReadOnlyCollection<SolutionComponent> componentList, IOrganizationServiceAsync2 crm);
	}
}
