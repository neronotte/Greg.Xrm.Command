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

		private CreateCommandExecutor NewExecutor() => new(OrganizationServiceRepositoryMock.Object, Output, new PublishXmlBuilder());
	}
}
