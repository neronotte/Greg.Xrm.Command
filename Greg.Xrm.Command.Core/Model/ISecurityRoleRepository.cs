using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public interface ISecurityRoleRepository
	{
		Task<SecurityRole> GetByIdentifierAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken);
		Task DeleteAsync(IOrganizationServiceAsync2 crm, Guid roleId, CancellationToken cancellationToken);
		Task<IReadOnlyList<string>> GetNamesAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken);
		Task<(Guid RoleId, int PrivilegeCount)> CloneAsync(IOrganizationServiceAsync2 crm, Guid sourceId,
			string name, string? description, Guid businessUnitId, int inheritance, CancellationToken cancellationToken);
		Task<SecurityRole> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, Guid businessUnitId, CancellationToken cancellationToken);
		Task<bool> IsAssignedAsync(IOrganizationServiceAsync2 crm, Guid roleId, EntityReference recipient, CancellationToken cancellationToken);
	}
}