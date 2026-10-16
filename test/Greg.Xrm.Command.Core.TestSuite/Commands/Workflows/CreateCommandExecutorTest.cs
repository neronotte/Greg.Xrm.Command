using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class CreateCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreateCommandExecutor executor;
		private readonly Mock<ISolutionRepository> solutionRepositoryMock = new();
		private readonly Mock<IWorkflowRepository> workflowRepositoryMock = new();
		private readonly Mock<IWorkflowDefinitionValidator> validatorMock = new();

		private readonly List<string> tempFiles = [];

		public CreateCommandExecutorTest()
		{
			this.validatorMock
				.Setup(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((CommandResult?)null);

			this.executor = new CreateCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.solutionRepositoryMock.Object,
				this.workflowRepositoryMock.Object,
				this.validatorMock.Object);
		}

		[TestCleanup]
		public void Cleanup()
		{
			foreach (var file in tempFiles)
			{
				try { File.Delete(file); } catch (IOException) { }
			}
		}


		private const string FlowDefinition = "{\"properties\":{\"connectionReferences\":{},\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}}}},\"schemaVersion\":\"1.0.0.0\"}";

		private class TestSolution : Model.Solution
		{
			public TestSolution(bool isManaged) : base(BuildEntity(isManaged)) { }

			private static Entity BuildEntity(bool isManaged)
			{
				var entity = new Entity("solution", Guid.NewGuid());
				entity["uniquename"] = "mysolution";
				entity["ismanaged"] = isManaged;
				return entity;
			}
		}

		private string WriteTempFile(string content)
		{
			var path = Path.Combine(Path.GetTempPath(), $"pacx-test-{Guid.NewGuid():N}.json");
			File.WriteAllText(path, content);
			tempFiles.Add(path);
			return path;
		}

		private static Workflow NamedWorkflow(string name)
		{
			var entity = new Entity("workflow", Guid.NewGuid());
			entity["name"] = name;
			return new Workflow(entity);
		}

		private void SetupSimilarWorkflows(params Workflow[] workflows)
		{
			this.workflowRepositoryMock
				.Setup(r => r.SearchByNameAndSolutionAndCategoryAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Workflow.Category?>()))
				.ReturnsAsync(workflows);
		}

		private void SetupHappyEnvironment()
		{
			this.solutionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync(new TestSolution(isManaged: false));

			this.workflowRepositoryMock
				.Setup(r => r.SearchByNameAndSolutionAndCategoryAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Workflow.Category?>()))
				.ReturnsAsync([]);

			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(Guid.NewGuid());

			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new AddSolutionComponentResponse());
		}


		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheFileDoesNotExist()
		{
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = Path.Combine(Path.GetTempPath(), "pacx-test-does-not-exist.json") };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheFileIsNotValidJson()
		{
			var file = WriteTempFile("this is not json");
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "valid json");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldCreateTheFlow_AsDraftModernFlow()
		{
			SetupHappyEnvironment();
			Entity? created = null;
			var newId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => created = Clone(e))
				.ReturnsAsync(newId);

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(created);
			Assert.AreEqual("workflow", created.LogicalName);
			Assert.AreEqual("My Flow", created.GetAttributeValue<string>("name"));
			Assert.AreEqual((int)Workflow.Category.ModernFlow, created.GetAttributeValue<OptionSetValue>("category").Value);
			Assert.AreEqual((int)Workflow.Type.Definition, created.GetAttributeValue<OptionSetValue>("type").Value);
			Assert.AreEqual("none", created.GetAttributeValue<string>("primaryentity"));
			StringAssert.Contains(created.GetAttributeValue<string>("clientdata"), "\"triggers\"", "The flow must carry the definition from the file.");
			Assert.IsFalse(created.GetAttributeValue<string>("clientdata").Contains("@false"), "The muzzle belongs to the validation probe only, never to the real flow.");
			Assert.AreEqual(newId, result["workflowid"]);
			this.validatorMock.Verify(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.Is<string>(c => c.Contains("\"triggers\"")), It.IsAny<CancellationToken>()), Times.Once, "The definition must be validated before the flow is created.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNotCreateAnything_WhenTheValidationFails()
		{
			SetupHappyEnvironment();
			this.validatorMock
				.Setup(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(CommandResult.Fail("The flow engine rejected the definition: invalid."));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "rejected");
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Never, "A rejected definition must never reach a real flow.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldCompactTheDefinition_AndKeepNonAsciiReadable()
		{
			SetupHappyEnvironment();
			Entity? created = null;
			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => created = Clone(e))
				.ReturnsAsync(Guid.NewGuid());

			var indented = "{\r\n  \"properties\": {\r\n    \"definition\": {\r\n      \"description\": \"Grün, it's fine\"\r\n    }\r\n  }\r\n}";
			var file = WriteTempFile(indented);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(created);
			var clientData = created.GetAttributeValue<string>("clientdata");
			Assert.IsFalse(clientData.Contains('\n'), "The definition must be stored compacted, the way the flow designer saves it.");
			StringAssert.Contains(clientData, "Grün, it's fine", "Escaping the definition would make it hard to read.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldAddTheFlowToTheSolution()
		{
			SetupHappyEnvironment();
			OrganizationRequest? executed = null;
			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Callback<OrganizationRequest, CancellationToken>((r, _) => executed = r)
				.ReturnsAsync(new AddSolutionComponentResponse());

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var request = executed as AddSolutionComponentRequest;
			Assert.IsNotNull(request, "The flow must be added to the solution.");
			Assert.AreEqual("mysolution", request.SolutionUniqueName);
			Assert.AreEqual(29, request.ComponentType, "29 is the solution component type of workflows.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUseTheDefaultSolution_WhenNoSolutionIsGiven()
		{
			SetupHappyEnvironment();
			this.OrganizationServiceRepositoryMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync("defaultsolution");

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.solutionRepositoryMock.Verify(r => r.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "defaultsolution"));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoSolutionIsGivenAndNoDefaultIsSet()
		{
			SetupHappyEnvironment();
			this.OrganizationServiceRepositoryMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync((string?)null);

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "--solution");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheSolutionIsManaged()
		{
			SetupHappyEnvironment();
			this.solutionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync(new TestSolution(isManaged: true));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "managed");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenAWorkflowWithTheSameNameExists()
		{
			SetupHappyEnvironment();
			SetupSimilarWorkflows(NamedWorkflow("My Flow"));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = " My Flow ", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "already exists");
			this.workflowRepositoryMock.Verify(r => r.SearchByNameAndSolutionAndCategoryAsync(It.IsAny<IOrganizationServiceAsync2>(), "My Flow", null, null), Times.Once, "The duplicate check must search the trimmed name across all solutions and categories.");
			this.validatorMock.Verify(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never, "The cheap duplicate check must run before the expensive validation probe.");
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldDetectTheDuplicate_WhenTheCasingDiffers()
		{
			SetupHappyEnvironment();
			SetupSimilarWorkflows(NamedWorkflow("MY FLOW"));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "my flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "already exists");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldDetectTheDuplicate_WhenTheExistingNameCarriesSpaces()
		{
			SetupHappyEnvironment();
			SetupSimilarWorkflows(NamedWorkflow(" My Flow"));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "already exists");
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNotDetectADuplicate_WhenOnlyALongerNameMatches()
		{
			SetupHappyEnvironment();
			SetupSimilarWorkflows(NamedWorkflow("My Flow With Longer Name"));

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWarnButSucceed_WhenTheJsonDoesNotLookLikeAFlow()
		{
			SetupHappyEnvironment();

			var file = WriteTempFile("{\"foo\":\"bar\"}");
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "does not look like the definition of a flow");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReturnTheWorkflowId()
		{
			SetupHappyEnvironment();
			var newId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(newId);

			var file = WriteTempFile(FlowDefinition);
			var command = new CreateCommand { Name = "My Flow", DefinitionFile = file, SolutionName = "mysolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(newId, result["workflowid"]);
		}
	}
}
