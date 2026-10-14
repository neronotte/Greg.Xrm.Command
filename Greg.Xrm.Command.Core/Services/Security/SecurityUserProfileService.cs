using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public sealed record SecurityProfileBusinessUnit(Guid Id, string Name);
	public sealed record SecurityTeamProfile(Guid TeamId, string Name, string Type, Guid? BusinessUnitId,
		string BusinessUnit, IReadOnlyList<SecurityRoleInfo> Roles);
	public sealed record SecurityUserProfile(Guid UserId, string FullName, string DomainName, SecurityProfileBusinessUnit? BusinessUnit,
		IReadOnlyList<SecurityRoleAssignmentInfo> Roles, IReadOnlyList<SecurityTeamProfile> Teams);

	public class SecurityUserProfileService(ISecurityUserResolver userResolver, ITeamRepository teams,
		ISecurityRoleService roles, IBusinessUnitRepository businessUnits) : ISecurityUserProfileService
	{
		public async Task<SecurityUserProfile> GetAsync(IOrganizationServiceAsync2 crm, string? identifier, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var user = await userResolver.ResolveAsync(crm, identifier, cancellationToken);
			SecurityProfileBusinessUnit? businessUnit = null;
			if (user.BusinessUnit != null)
			{
				var found = await businessUnits.GetByIdsAsync(crm, [user.BusinessUnit.Id], cancellationToken);
				businessUnit = new SecurityProfileBusinessUnit(user.BusinessUnit.Id,
					found.FirstOrDefault()?.name ?? user.BusinessUnit.Name ?? user.BusinessUnit.Id.ToString());
			}
			var membership = await teams.GetByUserAsync(crm, user.UserId, cancellationToken);
			var direct = await roles.GetDirectRolesByUserAsync(crm, user.UserId, cancellationToken);
			var byTeam = await roles.GetRolesByTeamsAsync(crm, membership.Select(team => team.Id), cancellationToken);
			var teamProfiles = membership.DistinctBy(team => team.Id).OrderBy(team => team.name, StringComparer.OrdinalIgnoreCase).ThenBy(team => team.Id)
				.Select(team => new SecurityTeamProfile(team.Id, team.name, team.TypeName, team.businessunitid?.Id,
					team.BusinessUnitName, byTeam.TryGetValue(team.Id, out var assigned) ? assigned : [])).ToArray();
			var assignments = direct.DistinctBy(role => role.RoleId)
				.Select(role => new SecurityRoleAssignmentInfo(role, ["Direct"]))
				.OrderBy(assignment => assignment.Role.Name, StringComparer.OrdinalIgnoreCase).ThenBy(assignment => assignment.Role.RoleId).ToArray();
			return new SecurityUserProfile(user.UserId, user.FullName, user.DomainName, businessUnit, assignments, teamProfiles);
		}
	}
}