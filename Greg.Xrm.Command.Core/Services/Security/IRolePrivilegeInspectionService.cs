using Greg.Xrm.Command.Commands.Security.Roles;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public interface IRolePrivilegeInspectionService
	{
		Task<RolePrivilegeSnapshot> InspectAsync(IOrganizationServiceAsync2 crm, Guid roleId, RolePrivilegeViewMode mode, string? tableFilter, string? privilegeFilter, CancellationToken cancellationToken);
	}
}