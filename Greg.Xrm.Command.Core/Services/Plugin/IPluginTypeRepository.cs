using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginTypeRepository
	{
		public Task<PluginType[]> GetByAssemblyId(IOrganizationServiceAsync2 crm, Guid assemblyId, CancellationToken cancellationToken);
	}
}
