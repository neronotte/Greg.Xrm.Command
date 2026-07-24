using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services
{
	public interface IObjectTypeCodeFinder
	{
		Task<int> GetObjectTypeCodeForTableAsync(IOrganizationServiceAsync2 crm, string tableLogicalName, CancellationToken cancellationToken);
	}
}
