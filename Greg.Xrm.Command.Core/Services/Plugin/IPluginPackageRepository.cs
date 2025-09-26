using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginPackageRepository
	{
		public Task<PluginPackage?> GetByIdAsync(IOrganizationServiceAsync2 crm, string packageId, CancellationToken cancellationToken);
	}
}
