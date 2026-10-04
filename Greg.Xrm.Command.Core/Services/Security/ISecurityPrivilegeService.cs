using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public interface ISecurityPrivilegeService
	{
		Task<IReadOnlyList<SecurityPrivilegeInfo>> CheckPrivilegesAsync(IOrganizationServiceAsync2 crm, Guid userId, string tableName, Guid? recordId, CancellationToken cancellationToken);
	}
}