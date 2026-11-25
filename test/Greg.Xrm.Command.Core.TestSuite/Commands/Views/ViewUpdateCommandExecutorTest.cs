using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Views;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ViewUpdateCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly Mock<IViewRetrieverService> retriever = new();
		private readonly Mock<IViewXmlEditor> editor = new(MockBehavior.Strict);
		private readonly Guid viewId = Guid.NewGuid();
		private const string OriginalFetch = "original fetch";
		private const string OriginalLayout = "original layout";

		public ViewUpdateCommandExecutorTest()
		{
			var entity = new Entity("savedquery", viewId)
			{
				["name"] = "Active", ["returnedtypecode"] = "account",
				["fetchxml"] = OriginalFetch, ["layoutxml"] = OriginalLayout
			};
			retriever.Setup(r => r.GetByNameAsync(OrganizationServiceMock.Object, QueryType1.SavedQuery, "Active", "account"))
				.ReturnsAsync((CommandResult.Success(), new SavedQuery(entity)));
			OrganizationServiceMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new OrganizationResponse());
		}

		[TestMethod]
		[DataRow("filter", false)]
		[DataRow("fetch", false)]
		[DataRow("columns", false)]
		[DataRow("both", false)]
		[DataRow("filter", true)]
		[DataRow("fetch", true)]
		[DataRow("columns", true)]
		[DataRow("both", true)]
		public async Task SavesInjectedEditorResultsAndReturnsScalarIdentifiers(string operation, bool publish)
		{
			editor.Setup(e => e.SetFilter(OriginalFetch, "filter")).Returns("updated fetch");
			editor.Setup(e => e.SetFetchXml(OriginalLayout, "fetch", "account")).Returns(("updated fetch", "updated layout"));
			editor.Setup(e => e.SetColumns(OriginalFetch, OriginalLayout, "columns")).Returns(("updated fetch", "updated layout"));
			editor.Setup(e => e.SetView("fetch", "layout", "account")).Returns(("updated fetch", "updated layout", new[] { "unused" }));
			Entity? saved = null;
			OrganizationServiceMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e =>
				{
					saved = new Entity(e.LogicalName, e.Id);
					saved.Attributes.AddRange(e.Attributes);
				})
				.Returns(Task.CompletedTask);
			using var cancellation = new CancellationTokenSource();

			var result = await ExecuteAsync(operation, publish, cancellation.Token);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(viewId, result["ViewId"]);
			Assert.AreEqual("Active", result["ViewName"]);
			Assert.AreEqual("account", result["TableName"]);
			Assert.AreEqual(publish, result["Published"]);
			OrganizationServiceMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
			Assert.IsNotNull(saved);
			Assert.AreEqual(viewId, saved.Id);
			Assert.AreEqual("updated fetch", saved.GetAttributeValue<string>("fetchxml"));
			if (operation == "filter") Assert.AreEqual(OriginalLayout, saved.GetAttributeValue<string>("layoutxml"));
			else Assert.AreEqual("updated layout", saved.GetAttributeValue<string>("layoutxml"));
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.Is<PublishXmlRequest>(r =>
				r.ParameterXml.Contains("<entity>account</entity>")), cancellation.Token), publish ? Times.Once() : Times.Never());
			Assert.AreEqual(1, editor.Invocations.Count);
			StringAssert.Contains(Output.ToString(), "Connecting to the current dataverse environment...");
			if (operation == "both") StringAssert.Contains(Output.ToString(), "unused");
		}

		[TestMethod]
		public async Task LookupFailureDoesNotEditSaveOrPublish()
		{
			var failure = CommandResult.Fail("View not found");
			retriever.Setup(r => r.GetByNameAsync(OrganizationServiceMock.Object, QueryType1.SavedQuery, "Active", "account"))
				.ReturnsAsync((failure, (TableView?)null));
			var result = await ExecuteAsync("filter", true);
			Assert.AreSame(failure, result);
			editor.VerifyNoOtherCalls();
			VerifyNoWrites();
		}

		[TestMethod]
		public async Task LookupFaultReturnsFailureWithoutWriting()
		{
			retriever.Setup(r => r.GetByNameAsync(OrganizationServiceMock.Object, QueryType1.SavedQuery, "Active", "account"))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Lookup failed"));
			var result = await ExecuteAsync("filter", true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Lookup failed");
			VerifyNoWrites();
		}

		[TestMethod]
		public async Task SaveFaultReturnsFailureAndDoesNotPublish()
		{
			editor.Setup(e => e.SetFilter(OriginalFetch, "filter")).Returns("updated fetch");
			OrganizationServiceMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Save failed"));
			var result = await ExecuteAsync("filter", true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Save failed");
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ConnectionFaultReturnsFailureWithoutWriting()
		{
			OrganizationServiceRepositoryMock.Setup(r => r.GetCurrentConnectionAsync()).ThrowsAsync(new InvalidOperationException("Connection failed"));
			var result = await ExecuteAsync("filter", true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Connection failed");
			VerifyNoWrites();
		}

		[TestMethod]
		public async Task CancellationDuringPublishPropagates()
		{
			editor.Setup(e => e.SetFilter(OriginalFetch, "filter")).Returns("updated fetch");
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), cancellation.Token))
				.ThrowsAsync(new OperationCanceledException(cancellation.Token));
			await Assert.ThrowsAsync<OperationCanceledException>(() => ExecuteAsync("filter", true, cancellation.Token));
		}

		private void VerifyNoWrites()
		{
			OrganizationServiceMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		private Task<CommandResult> ExecuteAsync(string operation, bool publish, CancellationToken token = default)
		{
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(p => p.Build()).Returns(new PublishXmlRequest { ParameterXml = "<entity>account</entity>" });
			return operation switch
			{
				"filter" => new SetFilterCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, retriever.Object, publisher.Object, editor.Object)
					.ExecuteAsync(new SetFilterCommand { ViewName = "Active", TableName = "account", Filter = "filter", Publish = publish }, token),
				"fetch" => new SetFetchXmlCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, retriever.Object, publisher.Object, editor.Object)
					.ExecuteAsync(new SetFetchXmlCommand { ViewName = "Active", TableName = "account", FetchXml = "fetch", Publish = publish }, token),
				"columns" => new SetColumnsCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, retriever.Object, publisher.Object, editor.Object)
					.ExecuteAsync(new SetColumnsCommand { ViewName = "Active", TableName = "account", Columns = "columns", Publish = publish }, token),
				_ => new SetCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, retriever.Object, publisher.Object, editor.Object)
					.ExecuteAsync(new SetCommand { ViewName = "Active", TableName = "account", FetchXml = "fetch", LayoutXml = "layout", Publish = publish }, token)
			};
		}
	}
}
