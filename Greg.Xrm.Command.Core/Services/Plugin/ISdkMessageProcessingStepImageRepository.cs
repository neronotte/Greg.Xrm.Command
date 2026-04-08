using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface ISdkMessageProcessingStepImageRepository
	{
		Task<SdkMessageProcessingStepImage[]> GetByStepIdAsync(IOrganizationServiceAsync2 crm, Guid stepId);
		Task<SdkMessageProcessingStepImage[]> GetByStepIdsAsync(IOrganizationServiceAsync2 crm, Guid[] stepIds);
		Task<SdkMessageProcessingStepImage[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken);
	}
}
