using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class AddCustomApiParamCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly AddCustomApiParamCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();

		public AddCustomApiParamCommandExecutorTest()
		{
			this.executor = new AddCustomApiParamCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
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

		private void SetupNoExistingParam()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiExistsAndParamIsNew()
		{
			SetupApiFound();
			SetupNoExistingParam();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")))
				.ReturnsAsync(Guid.NewGuid());

			var result = await executor.ExecuteAsync(
				new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "X:Integer" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSetIsOptional_WhenParamHasQuestionMark()
		{
			SetupApiFound();
			SetupNoExistingParam();

			// Capture the value before SaveOrUpdateAsync clears the entity's attributes
			bool capturedIsOptional = false;
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")))
				.Callback<Entity>(e => capturedIsOptional = e.GetAttributeValue<bool>("isoptional"))
				.ReturnsAsync(Guid.NewGuid());

			await executor.ExecuteAsync(
				new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "X?:String" },
				CancellationToken.None);

			Assert.IsTrue(capturedIsOptional);
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
				new AddCustomApiParamCommand { ApiUniqueName = "nn_Missing", Param = "nn_X:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenParamAlreadyExists()
		{
			SetupApiFound();

			var existingParam = new Entity("customapirequestparameter") { Id = Guid.NewGuid() };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existingParam }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existingParam }));

			var result = await executor.ExecuteAsync(
				new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "X:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupNoExistingParam();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "X:Integer" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
