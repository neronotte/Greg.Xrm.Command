using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class TranslateCommandExecutorTest
	{
		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateSingularPluralAndPublish()
		{
			var (output, repository, crm) = CreateMocks();
			var parser = new Mock<ITranslationParser>();
			parser.Setup(x => x.Parse("singular"))
				.Returns(new Dictionary<int, string> { [1033] = "Account", [1040] = "Azienda" });
			parser.Setup(x => x.Parse("plural"))
				.Returns(new Dictionary<int, string> { [1033] = "Accounts", [1040] = "Aziende" });
			var languages = new Mock<IActiveLanguageService>();
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(x => x.Build()).Returns(new PublishXmlRequest());
			OrganizationRequest? updateRequest = null;

			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					if (request is RetrieveEntityRequest)
						return new RetrieveEntityResponse
						{
							Results = { ["EntityMetadata"] = new EntityMetadata { LogicalName = "account" } },
						};
					if (request is UpdateEntityRequest)
						updateRequest = request;
					return request is PublishXmlRequest
						? new PublishXmlResponse()
						: new UpdateEntityResponse();
				});

			var executor = new TranslateCommandExecutor(output, repository.Object, parser.Object, languages.Object, publisher.Object);
			var result = await executor.ExecuteAsync(
				new TranslateCommand { TableName = "account", Singular = "singular", Plural = "plural" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(2, result["TranslatedLanguages"]);
			var request = (UpdateEntityRequest?)updateRequest;
			Assert.IsNotNull(request);
			Assert.IsTrue(request.MergeLabels);
			Assert.AreEqual(2, request.Entity.DisplayName.LocalizedLabels.Count);
			Assert.AreEqual(2, request.Entity.DisplayCollectionName.LocalizedLabels.Count);
			publisher.Verify(x => x.AddTable("account"), Times.Once);
		}

		private static (OutputToMemory output, Mock<IOrganizationServiceRepository> repository, Mock<IOrganizationServiceAsync2> crm) CreateMocks()
		{
			var output = new OutputToMemory();
			var repository = new Mock<IOrganizationServiceRepository>();
			var crm = new Mock<IOrganizationServiceAsync2>();
			repository.Setup(x => x.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			return (output, repository, crm);
		}
	}
}
