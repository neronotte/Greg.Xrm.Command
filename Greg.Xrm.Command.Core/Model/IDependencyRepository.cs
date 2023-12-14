using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IDependencyRepository
	{
		Task<DependencyList> GetDependenciesAsync(IOrganizationServiceAsync2 crm, ComponentType componentType, Guid componentId, bool? forDelete = false);
	}
}
