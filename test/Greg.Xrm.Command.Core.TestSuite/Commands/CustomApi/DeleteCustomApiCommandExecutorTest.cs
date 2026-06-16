using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class DeleteCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly DeleteCustomApiCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();

		public DeleteCustomApiCommandExecutorTest()
		{
			this.executor = new DeleteCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private void SetupApiFound()
		{
			var api = new Entity("customapi") { Id = ApiId };
			api["uniquename"] = "nn_GregSum";
			api["displayname"] = "Greg Sum";
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenForceAndApiExists()
		{
			SetupApiFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.Returns(Task.CompletedTask);

			var result = await executor.ExecuteAsync(
				new DeleteCustomApiCommand { UniqueName = "nn_GregSum", Force = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.DeleteAsync("customapi", ApiId),
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
				new DeleteCustomApiCommand { UniqueName = "nn_Missing", Force = true },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new DeleteCustomApiCommand { UniqueName = "nn_GregSum", Force = true },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
