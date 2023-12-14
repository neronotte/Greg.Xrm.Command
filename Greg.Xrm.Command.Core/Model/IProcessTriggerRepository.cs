using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IProcessTriggerRepository
	{
		Task<IReadOnlyList<ProcessTrigger>> GetByWorkflowIdAsync(IOrganizationServiceAsync2 crm, Guid workflowId);
	}
}
