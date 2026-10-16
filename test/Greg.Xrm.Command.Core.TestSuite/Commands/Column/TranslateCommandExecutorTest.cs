using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
	[TestClass]
	public class TranslateCommandExecutorTest
	{
		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateAllLabelsAndPublish()
		{
			var (output, repository, crm) = CreateMocks();
			var parser = new Mock<ITranslationParser>();
			parser
				.Setup(x => x.Parse(It.IsAny<string>()))
				.Returns(new Dictionary<int, string> { [1033] = "Customer", [1040] = "Cliente" });
			var languages = new Mock<IActiveLanguageService>();
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(x => x.Build()).Returns(new PublishXmlRequest());

			AttributeMetadata? metadata = new StringAttributeMetadata
			{
				LogicalName = "new_name",
				DisplayName = new Label("Old", 1033),
			};
			OrganizationRequest? updateRequest = null;
			crm
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					if (request is RetrieveAttributeRequest)
						return new RetrieveAttributeResponse
						{
							Results = { ["AttributeMetadata"] = metadata },
						};
					if (request is UpdateAttributeRequest)
						updateRequest = request;
					return request is PublishXmlRequest
						? new PublishXmlResponse()
						: new UpdateAttributeResponse();
				});

			var executor = new TranslateCommandExecutor(
				output,
				repository.Object,
				parser.Object,
				languages.Object,
				publisher.Object);

			var result = await executor.ExecuteAsync(
				new TranslateCommand
				{
					TableName = "account",
					ColumnName = "new_name",
					Labels = "ignored",
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(2, result["TranslatedLanguages"]);
			Assert.AreEqual(true, result["Published"]);
			Assert.IsInstanceOfType<UpdateAttributeRequest>(updateRequest);
			var request = (UpdateAttributeRequest)updateRequest!;
			Assert.IsTrue(request.MergeLabels);
			Assert.AreEqual(2, request.Attribute.DisplayName.LocalizedLabels.Count);
			languages.Verify(x => x.ValidateAsync(crm.Object, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Once);
			publisher.Verify(x => x.AddTable("account"), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNotPublishWhenRequested()
		{
			var (output, repository, crm) = CreateMocks();
			var parser = new Mock<ITranslationParser>();
			parser.Setup(x => x.Parse(It.IsAny<string>()))
				.Returns(new Dictionary<int, string> { [1033] = "Customer" });
			var languages = Mock.Of<IActiveLanguageService>();
			var publisher = new Mock<IPublishXmlBuilder>();
			crm
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new RetrieveAttributeResponse
				{
					Results =
					{
						["AttributeMetadata"] = new StringAttributeMetadata { LogicalName = "new_name" },
					},
				});

			var executor = new TranslateCommandExecutor(output, repository.Object, parser.Object, languages, publisher.Object);
			var result = await executor.ExecuteAsync(
				new TranslateCommand { TableName = "account", ColumnName = "new_name", Labels = "ignored", NoPublish = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(false, result["Published"]);
			publisher.Verify(x => x.AddTable(It.IsAny<string>()), Times.Never);
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
