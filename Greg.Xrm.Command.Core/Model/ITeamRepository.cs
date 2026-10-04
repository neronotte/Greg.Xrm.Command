using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface ITeamRepository
	{
		Task<IReadOnlyList<Team>> SearchAsync(IOrganizationServiceAsync2 crm, TeamType? type, string? name, CancellationToken cancellationToken);
		Task<IReadOnlyList<Team>> GetByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken);
		Task<Team> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken);
	}
}