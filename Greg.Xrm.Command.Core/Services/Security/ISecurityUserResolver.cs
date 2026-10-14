using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public interface ISecurityUserResolver
	{
		Task<SecurityUserInfo> ResolveAsync(IOrganizationServiceAsync2 crm, string? user, CancellationToken cancellationToken);
	}
}