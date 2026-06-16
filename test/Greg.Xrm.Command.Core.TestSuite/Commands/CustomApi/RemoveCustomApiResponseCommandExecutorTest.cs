using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RemoveCustomApiResponseCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly RemoveCustomApiResponseCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();
		private static readonly Guid ResponseId = Guid.NewGuid();

		public RemoveCustomApiResponseCommandExecutorTest()
		{
			this.executor = new RemoveCustomApiResponseCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
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

		private void SetupResponseFound()
		{
			var resp = new Entity("customapiresponseproperty") { Id = ResponseId };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { resp }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { resp }));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAndResponseExist()
		{
			SetupApiFound();
			SetupResponseFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.Returns(Task.CompletedTask);

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", ResponseUniqueName = "Result" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.DeleteAsync("customapiresponseproperty", ResponseId),
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
				new RemoveCustomApiResponseCommand { ApiUniqueName = "nn_Missing", ResponseUniqueName = "Result" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenResponseNotFound()
		{
			SetupApiFound();
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", ResponseUniqueName = "Missing" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupResponseFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", ResponseUniqueName = "Result" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
