using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginTypeRepository
	{
		Task<PluginType?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid id, CancellationToken cancellationToken);
		Task<PluginType[]> FuzzySearchAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken);
		Task<PluginType[]> GetByAssemblyId(IOrganizationServiceAsync2 crm, Guid assemblyId, CancellationToken cancellationToken);
		Task<PluginType[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken);
	}
}
