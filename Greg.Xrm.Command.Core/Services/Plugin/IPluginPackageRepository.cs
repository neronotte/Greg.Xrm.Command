using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginPackageRepository
	{
		Task<PluginPackage?> GetByIdAsync(IOrganizationServiceAsync2 crm, string packageId, CancellationToken cancellationToken);
		Task<PluginPackage[]> GetByGuidsAsync(IOrganizationServiceAsync2 crm, Guid[] ids, CancellationToken cancellationToken);
		Task<PluginPackage[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken);
	}
}
