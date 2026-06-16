using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CreateCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreateCustomApiCommandExecutor executor;

		public CreateCustomApiCommandExecutorTest()
		{
			this.executor = new CreateCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private void SetupNoExistingApi()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
		}

		private void SetupCreateReturnsNewId(string logicalName)
		{
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == logicalName)))
				.ReturnsAsync(Guid.NewGuid());
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == logicalName), It.IsAny<CancellationToken>()))
				.ReturnsAsync(Guid.NewGuid());
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenCreatingMinimalApi()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldCreateParamsAndResponses_WhenProvided()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			// Empty idempotency checks for param and response
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
			SetupCreateReturnsNewId("customapirequestparameter");
			SetupCreateReturnsNewId("customapiresponseproperty");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand
				{
					UniqueName = "nn_GregSum",
					Params = "Addend1:Integer,Addend2:Integer",
					Responses = "Result:Integer"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")),
				Times.Exactly(2));
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapiresponseproperty")),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAlreadyExists()
		{
			// Idempotency: existing API found → skip creation, return success
			var existing = new Entity("customapi") { Id = Guid.NewGuid() };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existing }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existing }));

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.IsAny<Entity>()),
				Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldInferDisplayName_WhenNotProvided()
		{
			SetupNoExistingApi();

			// Capture the value before SaveOrUpdateAsync clears the entity's attributes
			string? capturedDisplayName = null;
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
				.Callback<Entity>(e => capturedDisplayName = e.GetAttributeValue<string>("displayname"))
				.ReturnsAsync(Guid.NewGuid());

			await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.AreEqual("Greg Sum", capturedDisplayName);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupNoExistingApi();

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
