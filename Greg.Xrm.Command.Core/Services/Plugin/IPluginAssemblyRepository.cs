using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginAssemblyRepository
	{
		Task<PluginAssembly?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken);
		Task<PluginAssembly[]> GetByPackageIdAsync(IOrganizationServiceAsync2 crm, Guid packageId, CancellationToken cancellationToken);
		Task<PluginAssembly[]> GetByGuidsAsync(IOrganizationServiceAsync2 crm, Guid[] ids, CancellationToken cancellationToken);
		Task<PluginAssembly[]> GetBySolutionIdAsync(IOrganizationServiceAsync2 crm, Guid solutionId, CancellationToken cancellationToken);
		Task<PluginAssembly[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken);
	}
}
