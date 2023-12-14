using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
#pragma warning restore IDE1006 // Naming Styles


	public interface IWorkflowRepository
	{
		Task<IReadOnlyList<Workflow>> GetByIdsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> ids);
	}
}
