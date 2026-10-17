using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Translations
{
	public interface IActiveLanguageService
	{
		Task ValidateAsync(
			IOrganizationServiceAsync2 organizationService,
			IEnumerable<int> lcids,
			CancellationToken cancellationToken);
	}
}
