using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class TranslateCommandExecutorTest
	{
		[TestMethod]
		public async Task ExecuteAsync_ShouldSetNameDescriptionAndPublish()
		{
			var output = new OutputToMemory();
			var repository = new Mock<IOrganizationServiceRepository>();
			var crm = new Mock<IOrganizationServiceAsync2>();
			repository.Setup(x => x.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);

			var viewId = Guid.NewGuid();
			var view = new SavedQuery(new Entity("savedquery", viewId)
			{
				["name"] = "Active Accounts",
				["returnedtypecode"] = "account",
			});
			var retriever = new Mock<IViewRetrieverService>();
			retriever
				.Setup(x => x.GetByNameAsync(crm.Object, QueryType1.SavedQuery, "Active Accounts", "account"))
				.ReturnsAsync((CommandResult.Success(), view));

			var parser = new Mock<ITranslationParser>();
			parser.Setup(x => x.Parse("labels"))
				.Returns(new Dictionary<int, string> { [1033] = "Active Accounts", [1040] = "Aziende attive" });
			parser.Setup(x => x.Parse("description"))
				.Returns(new Dictionary<int, string> { [1033] = "Active customer accounts" });
			var languages = new Mock<IActiveLanguageService>();
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(x => x.Build()).Returns(new PublishXmlRequest());
			var localizationRequests = new List<SetLocLabelsRequest>();
			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					if (request is SetLocLabelsRequest localizationRequest)
						localizationRequests.Add(localizationRequest);
					return request is PublishXmlRequest
						? new PublishXmlResponse()
						: new SetLocLabelsResponse();
				});

			var executor = new TranslateCommandExecutor(
				output,
				repository.Object,
				retriever.Object,
				parser.Object,
				languages.Object,
				publisher.Object);
			var result = await executor.ExecuteAsync(
				new TranslateCommand
				{
					Name = "Active Accounts",
					TableName = "account",
					Labels = "labels",
					Description = "description",
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(2, localizationRequests);
			Assert.IsTrue(localizationRequests.Any(x => x.AttributeName == "name" && x.Labels.Length == 2));
			Assert.IsTrue(localizationRequests.Any(x => x.AttributeName == "description" && x.Labels.Length == 1));
			publisher.Verify(x => x.AddTable("account"), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReportView_WhenSecondTranslationFails()
		{
			var output = new OutputToMemory();
			var repository = new Mock<IOrganizationServiceRepository>();
			var crm = new Mock<IOrganizationServiceAsync2>();
			repository.Setup(x => x.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			var view = new SavedQuery(new Entity("savedquery", Guid.NewGuid())
			{
				["name"] = "Active Accounts",
				["returnedtypecode"] = "account",
			});
			var retriever = new Mock<IViewRetrieverService>();
			retriever.Setup(x => x.GetByNameAsync(crm.Object, QueryType1.SavedQuery, "Active Accounts", "account"))
				.ReturnsAsync((CommandResult.Success(), view));
			var parser = new Mock<ITranslationParser>();
			parser.Setup(x => x.Parse(It.IsAny<string>()))
				.Returns(new Dictionary<int, string> { [1033] = "Translation" });
			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new SetLocLabelsResponse());
			crm.Setup(x => x.ExecuteAsync(It.Is<SetLocLabelsRequest>(r => r.AttributeName == "description"), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("Description update failed"));
			var executor = new TranslateCommandExecutor(output, repository.Object, retriever.Object, parser.Object, Mock.Of<IActiveLanguageService>(), Mock.Of<IPublishXmlBuilder>());

			var result = await executor.ExecuteAsync(new TranslateCommand { Name = "Active Accounts", TableName = "account", Labels = "labels", Description = "description" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(output.ToString(), "Editor URL unavailable for view 'Active Accounts'");
		}
	}
}
