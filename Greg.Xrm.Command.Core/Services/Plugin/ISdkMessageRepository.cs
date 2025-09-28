using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface ISdkMessageRepository
	{
		Task<SdkMessage[]> GetAllAsync(IOrganizationServiceAsync2 crm);
		Task<SdkMessage?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name);
	}
}
