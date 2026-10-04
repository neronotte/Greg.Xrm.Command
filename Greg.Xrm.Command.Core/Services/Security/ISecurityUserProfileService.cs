using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Security
{
	public interface ISecurityUserProfileService
	{
		Task<SecurityUserProfile> GetAsync(IOrganizationServiceAsync2 crm, string? identifier, CancellationToken cancellationToken);
	}
}