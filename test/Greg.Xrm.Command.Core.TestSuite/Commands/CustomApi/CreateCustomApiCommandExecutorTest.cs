using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CreateCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreateCustomApiCommandExecutor executor;
		private readonly Mock<ISolutionRepository> solutionRepositoryMock;

		private const string DefaultSolution = "TestSolution";

		public CreateCustomApiCommandExecutorTest()
		{
			this.solutionRepositoryMock = new Mock<ISolutionRepository>();

			// Default solution
			this.OrganizationServiceRepositoryMock
				.Setup(x => x.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync(DefaultSolution);

			// Solution lookup returns an unmanaged solution by default
			// Solution has a protected ctor; use reflection to create an instance for tests.
			var solEntity = new Entity("solution") { Id = Guid.NewGuid() };
			solEntity["ismanaged"] = false;
			var sol = (Greg.Xrm.Command.Model.Solution)Activator.CreateInstance(
				typeof(Greg.Xrm.Command.Model.Solution),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				null, new object[] { solEntity }, null)!;
			this.solutionRepositoryMock
				.Setup(x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync(sol);

			// AddSolutionComponentRequest always succeeds
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>()))
				.ReturnsAsync(new AddSolutionComponentResponse());
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new AddSolutionComponentResponse());

			this.executor = new CreateCustomApiCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.solutionRepositoryMock.Object);
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
		public async Task ExecuteAsync_ShouldAddApiToSolution_WhenCreated()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(DefaultSolution, result["Solution"]);

			this.OrganizationServiceMock.Verify(
				x => x.ExecuteAsync(It.Is<AddSolutionComponentRequest>(r =>
					r.SolutionUniqueName == DefaultSolution &&
						r.ComponentType == (int)ComponentType.CustomAPI), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUseSolutionFromCommand_WhenProvided()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum", SolutionName = "MySolution" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("MySolution", result["Solution"]);

			this.solutionRepositoryMock.Verify(
				x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "MySolution"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenSolutionNotFound()
		{
			this.solutionRepositoryMock
				.Setup(x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync((Greg.Xrm.Command.Model.Solution?)null);

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldCreateParamsAndResponses_WhenProvided()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

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
					Params     = "Addend1:Integer,Addend2:Integer",
					Responses  = "Result:Integer"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")),
				Times.Exactly(2));
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapiresponseproperty")),
				Times.Once);
			// All 4 components added to solution: 1 api + 2 params + 1 response
			this.OrganizationServiceMock.Verify(
					x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()),
				Times.Exactly(4));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAlreadyExists()
		{
			// Idempotency: existing API found -> skip creation, return success
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
		public async Task ExecuteAsync_ShouldFail_WhenNoSolutionNameAndNoDefault()
		{
			this.OrganizationServiceRepositoryMock
				.Setup(x => x.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync((string?)null);

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No solution name");
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
