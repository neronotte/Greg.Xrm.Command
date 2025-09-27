using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginAssemblyRepository
	{
		Task<PluginAssembly?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken);

		Task<PluginAssembly[]> GetByPackageIdAsync(IOrganizationServiceAsync2 crm, Guid packageId, CancellationToken cancellationToken);
	}
}
