using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
#pragma warning restore IDE1006 // Naming Styles


	public interface IWorkflowRepository
	{
		Task<IReadOnlyList<Workflow>> GetByIdsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> ids);
		Task<IReadOnlyList<Workflow>> GetByNameAsync(IOrganizationServiceAsync2 crm, string uniqueName);
		Task<IReadOnlyList<Workflow>> GetBySolutionAsync(IOrganizationServiceAsync2 crm, string solutionUniqueName);

		Task<IReadOnlyList<Workflow>> SearchByNameAndSolutionAndCategoryAsync(IOrganizationServiceAsync2 crm, string namePart, string? solutionUniqueName, Workflow.Category? category);
	}
}
