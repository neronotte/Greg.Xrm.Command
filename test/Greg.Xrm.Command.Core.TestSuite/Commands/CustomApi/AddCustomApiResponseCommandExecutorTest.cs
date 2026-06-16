using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class AddCustomApiResponseCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly AddCustomApiResponseCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();

		public AddCustomApiResponseCommandExecutorTest()
		{
			this.executor = new AddCustomApiResponseCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private void SetupApiFound()
		{
			var api = new Entity("customapi") { Id = ApiId };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
		}

		private void SetupNoExistingResponse()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiExistsAndResponseIsNew()
		{
			SetupApiFound();
			SetupNoExistingResponse();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapiresponseproperty")))
				.ReturnsAsync(Guid.NewGuid());

			var result = await executor.ExecuteAsync(
				new AddCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", Response = "Result:Integer" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapiresponseproperty")),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenApiNotFound()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new AddCustomApiResponseCommand { ApiUniqueName = "nn_Missing", Response = "Result:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenResponseAlreadyExists()
		{
			SetupApiFound();

			var existingResp = new Entity("customapiresponseproperty") { Id = Guid.NewGuid() };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existingResp }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existingResp }));

			var result = await executor.ExecuteAsync(
				new AddCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", Response = "Result:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupNoExistingResponse();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new AddCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", Response = "Result:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
