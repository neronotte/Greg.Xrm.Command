using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Translations
{
	public sealed class ActiveLanguageService : IActiveLanguageService
	{
		public async Task ValidateAsync(
			IOrganizationServiceAsync2 organizationService,
			IEnumerable<int> lcids,
			CancellationToken cancellationToken)
		{
			var requested = lcids.Distinct().ToArray();
			var activeInstalled = await GetActiveInstalledLcidsAsync(organizationService, cancellationToken);
			var invalid = requested.Where(lcid => !activeInstalled.Contains(lcid)).ToArray();

			if (invalid.Length == 0)
				return;

			throw new CommandException(
				CommandException.CommandInvalidArgumentValue,
				$"The following LCIDs are not installed and active in the current Dataverse organization: {string.Join(", ", invalid)}.");
		}

		private static async Task<HashSet<int>> GetActiveInstalledLcidsAsync(
			IOrganizationServiceAsync2 organizationService,
			CancellationToken cancellationToken)
		{
			var languageQuery = new QueryExpression("languagelocale")
			{
				ColumnSet = new ColumnSet("localeid"),
			};
			languageQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

			var activeLanguages = await organizationService.RetrieveMultipleAsync(languageQuery);
			var activeLcids = activeLanguages.Entities
				.Select(entity => entity.GetAttributeValue<int>("localeid"))
				.ToHashSet();

			var organizationQuery = new QueryExpression("organization")
			{
				ColumnSet = new ColumnSet("languagecode"),
				TopCount = 1,
			};
			var organizationResult = await organizationService.RetrieveMultipleAsync(organizationQuery);
			if (organizationResult.Entities.Count == 0)
				throw new CommandException(
					CommandException.XrmError,
					"The current Dataverse organization did not return its base language.");

			var baseLcid = organizationResult.Entities[0].GetAttributeValue<int>("languagecode");
			var installedResponse = (RetrieveInstalledLanguagePacksResponse)await organizationService.ExecuteAsync(
				new RetrieveInstalledLanguagePacksRequest());
			var provisionedResponse = (RetrieveProvisionedLanguagesResponse)await organizationService.ExecuteAsync(
				new RetrieveProvisionedLanguagesRequest());

			var installedLcids = installedResponse.RetrieveInstalledLanguagePacks.ToHashSet();
			var provisionedLcids = provisionedResponse.RetrieveProvisionedLanguages.ToHashSet();

			installedLcids.Add(baseLcid);
			provisionedLcids.Add(baseLcid);
			activeLcids.Add(baseLcid);

			activeLcids.IntersectWith(installedLcids);
			activeLcids.IntersectWith(provisionedLcids);
			return activeLcids;
		}
	}
}
