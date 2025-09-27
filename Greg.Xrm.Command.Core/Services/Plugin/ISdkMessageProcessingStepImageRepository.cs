using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Plugin
{
	public interface ISdkMessageProcessingStepImageRepository
	{
		Task<SdkMessageProcessingStepImage[]> GetByStepIdAsync(IOrganizationServiceAsync2 crm, Guid stepId);
	}
}
