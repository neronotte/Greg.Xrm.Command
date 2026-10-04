using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public interface ISecurityRoleService
	{
		Task<IReadOnlyList<SecurityRoleInfo>> GetRolesByIdentifierAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken);
		Task<IReadOnlyList<SecurityRoleInfo>> GetRolesAsync(IOrganizationServiceAsync2 crm, bool unmanagedOnly, CancellationToken cancellationToken);
		Task<IReadOnlyList<SecurityRoleInfo>> GetRolesAsync(IOrganizationServiceAsync2 crm, bool unmanagedOnly, string? nameFilter, CancellationToken cancellationToken);
		Task<IReadOnlyList<SecurityRoleAssignmentInfo>> GetRolesByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken);
		Task<IReadOnlyDictionary<Guid, IReadOnlyList<SecurityRoleInfo>>> GetRolesByTeamsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> teamIds, CancellationToken cancellationToken);
		Task<IReadOnlyList<SecurityRoleInfo>> GetDirectRolesByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken);
	}
}