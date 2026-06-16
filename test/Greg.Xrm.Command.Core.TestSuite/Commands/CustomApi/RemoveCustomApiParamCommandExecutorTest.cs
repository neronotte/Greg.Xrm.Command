using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RemoveCustomApiParamCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly RemoveCustomApiParamCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();
		private static readonly Guid ParamId = Guid.NewGuid();

		public RemoveCustomApiParamCommandExecutorTest()
		{
			this.executor = new RemoveCustomApiParamCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
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

		private void SetupParamFound()
		{
			var param = new Entity("customapirequestparameter") { Id = ParamId };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { param }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { param }));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAndParamExist()
		{
			SetupApiFound();
			SetupParamFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.Returns(Task.CompletedTask);

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiParamCommand { ApiUniqueName = "nn_GregSum", ParamUniqueName = "Addend1" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.DeleteAsync("customapirequestparameter", ParamId),
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
				new RemoveCustomApiParamCommand { ApiUniqueName = "nn_Missing", ParamUniqueName = "Addend1" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenParamNotFound()
		{
			SetupApiFound();
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiParamCommand { ApiUniqueName = "nn_GregSum", ParamUniqueName = "Missing" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupParamFound();
			this.OrganizationServiceMock
				.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new RemoveCustomApiParamCommand { ApiUniqueName = "nn_GregSum", ParamUniqueName = "Addend1" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
