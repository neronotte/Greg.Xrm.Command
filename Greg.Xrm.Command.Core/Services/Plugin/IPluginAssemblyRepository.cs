using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginAssemblyRepository
	{
		Task<PluginAssembly[]> GetByPackageIdAsync(IOrganizationServiceAsync2 crm, Guid packageId, CancellationToken cancellationToken);
	}
}
