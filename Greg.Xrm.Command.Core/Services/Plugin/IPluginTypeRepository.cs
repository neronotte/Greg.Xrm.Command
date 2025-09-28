using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginTypeRepository
	{
		Task<PluginType[]> FuzzySearchAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken);
		public Task<PluginType[]> GetByAssemblyId(IOrganizationServiceAsync2 crm, Guid assemblyId, CancellationToken cancellationToken);
	}
}
