using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface ISdkMessageProcessingStepRepository
	{
		Task<SdkMessageProcessingStep?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid id);
		Task<SdkMessageProcessingStep[]> GetByKeyAsync(IOrganizationServiceAsync2 crm, PluginType pluginType, string messageName, string? primaryEntityName, PluginRegistrationToolkit.Stage stage);
	}
}
