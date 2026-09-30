using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class SetCommandExecutorTest
	{
		private const string Fetch = "<fetch><entity name='account'><attribute name='name'/><attribute name='telephone1'/><attribute name='accountid'/></entity></fetch>";
		private const string OriginalLayout = "<grid><row id='accountid'><cell name='name' width='150'/></row></grid>";

		[TestMethod]
		public async Task MissingColumnDoesNotSaveOrPublish()
		{
			var (executor, crm, _) = BuildExecutor();
			var result = await executor.ExecuteAsync(new SetCommand
			{
				ViewName = "My View",
				FetchXml = Fetch,
				LayoutXml = "<grid><row id='accountid'><cell name='missing'/></row></grid>"
			}, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			crm.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never());
			crm.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>()), Times.Never());
		}

		[TestMethod]
		public async Task SavesValidLayoutWithoutPublishingByDefaultAndWarnsAboutUnusedFetchAttribute()
		{
			var (executor, crm, output) = BuildExecutor();
			var result = await executor.ExecuteAsync(new SetCommand
			{
				ViewName = "My View",
				FetchXml = Fetch,
				LayoutXml = "<grid><row id='accountid'><cell name='name' width='200'/></row></grid>"
			}, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess);
			crm.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once());
			crm.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>()), Times.Never());
			StringAssert.Contains(output.ToString(), "telephone1");
		}

		[TestMethod]
		public async Task PublishesOnlyTheViewTableWhenRequested()
		{
			var (executor, crm, _) = BuildExecutor();
			var result = await executor.ExecuteAsync(new SetCommand
			{
				ViewName = "My View", FetchXml = Fetch, Publish = true,
				LayoutXml = "<grid><row id='accountid'><cell name='name' width='200'/></row></grid>"
			}, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess);
			crm.Verify(c => c.ExecuteAsync(It.Is<PublishXmlRequest>(request =>
				request.ParameterXml.Contains("<entity>account</entity>"))), Times.Once());
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task SetFilterPublishesOnlyWhenRequested(bool publish)
		{
			var (connection, retriever, crm, output) = BuildDependencies();
			var executor = new SetFilterCommandExecutor(connection.Object, output, retriever.Object, new PublishXmlBuilder());
			var result = await executor.ExecuteAsync(new SetFilterCommand
			{
				ViewName = "My View", Publish = publish,
				Filter = "<filter><condition attribute='statecode' operator='eq' value='0'/></filter>"
			}, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			crm.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once());
			crm.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>()), publish ? Times.Once() : Times.Never());
		}

		private static (SetCommandExecutor Executor, Mock<IOrganizationServiceAsync2> Crm, OutputToMemory Output) BuildExecutor()
		{
			var (connection, retriever, crm, output) = BuildDependencies();
			return (new SetCommandExecutor(connection.Object, output, retriever.Object, new PublishXmlBuilder()), crm, output);
		}

		private static (Mock<IOrganizationServiceRepository> Connection, Mock<IViewRetrieverService> Retriever, Mock<IOrganizationServiceAsync2> Crm, OutputToMemory Output) BuildDependencies()
		{
			var crm = new Mock<IOrganizationServiceAsync2>();
			crm.Setup(c => c.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			crm.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>())).ReturnsAsync(new OrganizationResponse());
			var connection = new Mock<IOrganizationServiceRepository>();
			connection.Setup(c => c.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			var viewEntity = new Entity("savedquery", Guid.NewGuid());
			viewEntity["name"] = "My View";
			viewEntity["returnedtypecode"] = "account";
			viewEntity["fetchxml"] = Fetch;
			viewEntity["layoutxml"] = OriginalLayout;
			var view = new SavedQuery(viewEntity);
			var retriever = new Mock<IViewRetrieverService>();
			retriever.Setup(r => r.GetByNameAsync(crm.Object, QueryType1.SavedQuery, "My View", null))
				.ReturnsAsync((CommandResult.Success(), (TableView)view));
			return (connection, retriever, crm, new OutputToMemory());
		}
	}
}
