using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[TestClass]
	public class TranslateCommandExecutorTest
	{
		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateExistingGlobalOptionAndPublish()
		{
			var (output, repository, crm) = CreateMocks();
			var parser = new Mock<ITranslationParser>();
			parser.Setup(x => x.Parse(It.IsAny<string>()))
				.Returns(new Dictionary<int, string> { [1033] = "Pending", [1040] = "In attesa" });
			var languages = new Mock<IActiveLanguageService>();
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(x => x.Build()).Returns(new PublishXmlRequest());
			OrganizationRequest? updateRequest = null;
			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					if (request is RetrieveOptionSetRequest)
						return new RetrieveOptionSetResponse
						{
							Results =
							{
								["OptionSetMetadata"] = new OptionSetMetadata
								{
									Options = { new OptionMetadata(new Label("Pending", 1033), 100000000) },
								},
							},
						};
					if (request is UpdateOptionValueRequest)
						updateRequest = request;
					return request is PublishXmlRequest
						? new PublishXmlResponse()
						: new UpdateOptionValueResponse();
				});

			var executor = new TranslateCommandExecutor(output, repository.Object, parser.Object, languages.Object, publisher.Object);
			var result = await executor.ExecuteAsync(
				new TranslateCommand { Name = "new_status", Value = 100000000, Labels = "ignored" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var request = (UpdateOptionValueRequest?)updateRequest;
			Assert.IsNotNull(request);
			Assert.IsTrue(request.MergeLabels);
			Assert.AreEqual(2, request.Label.LocalizedLabels.Count);
			publisher.Verify(x => x.AddGlobalOptionSet("new_status"), Times.Once);
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
