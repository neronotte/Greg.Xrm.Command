using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface ISdkMessageProcessingStepRepository
	{
		Task<SdkMessageProcessingStep?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid id);
		Task<SdkMessageProcessingStep[]> GetByKeyAsync(IOrganizationServiceAsync2 crm, PluginType pluginType, string messageName, string? primaryEntityName, PluginRegistrationToolkit.Stage? stage = null);
		Task<SdkMessageProcessingStep[]> GetByAssemblyNameAsync(IOrganizationServiceAsync2 crm, string assemblyName, bool includeInternalStages, CancellationToken cancellationToken);
		Task<SdkMessageProcessingStep[]> GetByAssemblyIdAsync(IOrganizationServiceAsync2 crm, Guid assemblyId, bool includeInternalStages, CancellationToken cancellationToken);
		Task<SdkMessageProcessingStep[]> GetByPluginTypeIdAsync(IOrganizationServiceAsync2 crm, Guid pluginTypeId, bool includeInternalStages, CancellationToken cancellationToken);
		Task<SdkMessageProcessingStep[]> GetByTableNameAsync(IOrganizationServiceAsync2 crm, string tableName, bool includeInternalStages, CancellationToken cancellationToken);
		Task<SdkMessageProcessingStep[]> GetBySolutionAsync(IOrganizationServiceAsync2 crm, Guid solutionId, bool includeInternalStages, CancellationToken cancellationToken);
	}
}
