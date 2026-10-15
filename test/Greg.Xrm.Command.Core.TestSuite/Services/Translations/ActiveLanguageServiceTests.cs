using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Translations
{
	[TestClass]
	public class ActiveLanguageServiceTests
	{
		[TestMethod]
		public async Task ValidateAsync_ShouldAcceptInstalledProvisionedActiveLanguages()
		{
			var service = CreateOrganizationService(
				activeLcids: [1033, 1040],
				installedLcids: [1033, 1040],
				provisionedLcids: [1033, 1040],
				baseLcid: 1033);

			await new ActiveLanguageService().ValidateAsync(
				service.Object,
				[1033, 1040],
				CancellationToken.None);
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldAcceptBaseLanguageWhenSdkOmitsIt()
		{
			var service = CreateOrganizationService(
				activeLcids: [1040],
				installedLcids: [1040],
				provisionedLcids: [1040],
				baseLcid: 1033);

			await new ActiveLanguageService().ValidateAsync(
				service.Object,
				[1033],
				CancellationToken.None);
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldRejectLanguageNotInstalled()
		{
			var service = CreateOrganizationService(
				activeLcids: [1033, 1040],
				installedLcids: [1033],
				provisionedLcids: [1033, 1040],
				baseLcid: 1033);

			var exception = await Assert.ThrowsExactlyAsync<CommandException>(() =>
				new ActiveLanguageService().ValidateAsync(
					service.Object,
					[1040],
					CancellationToken.None));

			StringAssert.Contains(exception.Message, "1040");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldRejectLanguageNotActive()
		{
			var service = CreateOrganizationService(
				activeLcids: [1033],
				installedLcids: [1033, 1040],
				provisionedLcids: [1033, 1040],
				baseLcid: 1033);

			await Assert.ThrowsExactlyAsync<CommandException>(() =>
				new ActiveLanguageService().ValidateAsync(
					service.Object,
					[1040],
					CancellationToken.None));
		}

		private static Mock<IOrganizationServiceAsync2> CreateOrganizationService(
			IEnumerable<int> activeLcids,
			IEnumerable<int> installedLcids,
			IEnumerable<int> provisionedLcids,
			int baseLcid)
		{
			var service = new Mock<IOrganizationServiceAsync2>();
			service
				.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync((QueryBase query) =>
				{
					if (query is QueryExpression expression && expression.EntityName == "languagelocale")
						return new EntityCollection(activeLcids.Select(lcid =>
						{
							var entity = new Entity("languagelocale");
							entity["localeid"] = lcid;
							return entity;
						}).ToList());

					if (query is QueryExpression organizationQuery && organizationQuery.EntityName == "organization")
						return new EntityCollection([new Entity("organization") { ["languagecode"] = baseLcid }]);

					throw new InvalidOperationException($"Unexpected query: {query.GetType().Name}");
				});
			service
				.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest request) =>
				{
					if (request is RetrieveInstalledLanguagePacksRequest)
						return new RetrieveInstalledLanguagePacksResponse
						{
							Results = { ["RetrieveInstalledLanguagePacks"] = installedLcids.ToArray() }
						};

					if (request is RetrieveProvisionedLanguagesRequest)
						return new RetrieveProvisionedLanguagesResponse
						{
							Results = { ["RetrieveProvisionedLanguages"] = provisionedLcids.ToArray() }
						};

					throw new InvalidOperationException($"Unexpected request: {request.GetType().Name}");
				});

			return service;
		}
	}
}
