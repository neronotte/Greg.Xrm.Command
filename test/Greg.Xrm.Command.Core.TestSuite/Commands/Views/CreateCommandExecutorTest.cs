using Greg.Xrm.Command.Services.Views;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class CreateCommandExecutorTest : CommandExecutorTestBase
	{
		private const string Fetch = "<fetch><entity name='account'><attribute name='name'/></entity></fetch>";

		public CreateCommandExecutorTest()
		{
			var metadata = new EntityMetadata { LogicalName = "account" };
			typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.PrimaryIdAttribute))!.SetValue(metadata, "accountid");
			typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.PrimaryNameAttribute))!.SetValue(metadata, "name");
			typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.ObjectTypeCode))!.SetValue(metadata, 1);
			var response = new RetrieveEntityResponse();
			response.Results["EntityMetadata"] = metadata;

			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new OrganizationResponse());
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveEntityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
			OrganizationServiceMock.Setup(c => c.CreateAsync(It.IsAny<Entity>())).ReturnsAsync(Guid.NewGuid());
		}

		[TestMethod]
		[DataRow(QueryType1.SavedQuery, "savedquery")]
		[DataRow(QueryType1.UserQuery, "userquery")]
		public async Task CreatesViewWithoutAddingItToSolutionOrPublishing(QueryType1 queryType, string entityName)
		{
			var result = await NewExecutor().ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch, QueryType = queryType }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			OrganizationServiceMock.Verify(c => c.CreateAsync(It.Is<Entity>(e => e.LogicalName == entityName)), Times.Once());
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()), Times.Never());
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never());
		}

		[TestMethod]
		public async Task PublishesTableWhenRequested()
		{
			var result = await NewExecutor().ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch, Publish = true }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.Is<PublishXmlRequest>(r =>
				r.ParameterXml.Contains("<entity>account</entity>")), It.IsAny<CancellationToken>()), Times.Once());
		}

		private CreateCommandExecutor NewExecutor() => new(OrganizationServiceRepositoryMock.Object, Output, new PublishXmlBuilder(), CreateEditor().Object);
		private static Mock<IViewXmlEditor> CreateEditor()
		{
			var editor = new Mock<IViewXmlEditor>();
			editor.Setup(e => e.GetTableName(Fetch)).Returns("account");
			editor.Setup(e => e.CreateFromFetchXml(Fetch, It.IsAny<EntityMetadata>()))
				.Returns((Fetch, "<grid><row id='accountid'><cell name='name'/></row></grid>"));
			return editor;
		}

		[TestMethod]
		public async Task ReturnsCreatedIdentifierAndReportsConnectionProgress()
		{
			var id = Guid.NewGuid();
			Entity? created = null;
			OrganizationServiceMock.Setup(c => c.CreateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e =>
				{
					created = new Entity(e.LogicalName);
					created.Attributes.AddRange(e.Attributes);
				})
				.ReturnsAsync(id);
			var editor = CreateEditor();
			var executor = new CreateCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, new PublishXmlBuilder(), editor.Object);
			var result = await executor.ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(id, result["ViewId"]);
			Assert.AreEqual("Active", result["ViewName"]);
			Assert.AreEqual("account", result["TableName"]);
			Assert.AreEqual(false, result["Published"]);
			StringAssert.Contains(Output.ToString(), "Connecting to the current dataverse environment...");
			editor.Verify(e => e.CreateFromFetchXml(Fetch, It.IsAny<EntityMetadata>()), Times.Once);
			Assert.IsNotNull(created);
			Assert.AreEqual(Fetch, created.GetAttributeValue<string>("fetchxml"));
			StringAssert.Contains(created.GetAttributeValue<string>("layoutxml"), "accountid");
		}

		[TestMethod]
		public async Task InvalidDefinitionDoesNotCreateOrPublish()
		{
			var editor = CreateEditor();
			editor.Setup(e => e.CreateFromFetchXml(Fetch, It.IsAny<EntityMetadata>())).Throws(new ArgumentException("Invalid columns"));
			var executor = new CreateCommandExecutor(OrganizationServiceRepositoryMock.Object, Output, new PublishXmlBuilder(), editor.Object);
			var result = await executor.ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch, Publish = true }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Invalid columns");
			OrganizationServiceMock.Verify(c => c.CreateAsync(It.IsAny<Entity>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task DataverseFaultDoesNotPublish()
		{
			OrganizationServiceMock.Setup(c => c.CreateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Create failed"));
			var result = await NewExecutor().ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch, Publish = true }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Create failed");
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ConnectionFailureReturnsCommandFailure()
		{
			OrganizationServiceRepositoryMock.Setup(c => c.GetCurrentConnectionAsync()).ThrowsAsync(new InvalidOperationException("Connection failed"));
			var result = await NewExecutor().ExecuteAsync(new CreateCommand { ViewName = "Active", FetchXml = Fetch }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "Connection failed");
		}

	}
}
