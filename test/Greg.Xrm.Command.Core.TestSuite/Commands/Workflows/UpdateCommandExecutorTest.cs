using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class UpdateCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly UpdateCommandExecutor executor;
		private readonly Mock<IWorkflowRepository> workflowRepositoryMock = new();
		private readonly Mock<IWorkflowDefinitionValidator> validatorMock = new();

		private readonly List<string> tempFiles = [];

		public UpdateCommandExecutorTest()
		{
			this.validatorMock
				.Setup(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((CommandResult?)null);

			this.executor = new UpdateCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
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


		private const string NewDefinition = "{\"properties\":{\"connectionReferences\":{},\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"changed\":{\"type\":\"Compose\",\"inputs\":\"new\"}}}},\"schemaVersion\":\"1.0.0.0\"}";

		private string WriteTempFile(string content)
		{
			var path = Path.Combine(Path.GetTempPath(), $"pacx-test-{Guid.NewGuid():N}.json");
			File.WriteAllText(path, content);
			tempFiles.Add(path);
			return path;
		}

		private static Workflow CreateWorkflow(
			Workflow.Category category = Workflow.Category.ModernFlow,
			string? clientData = "{\"properties\":{\"definition\":{\"actions\":{}}}}",
			Workflow.State state = Workflow.State.Draft,
			string name = "My Flow")
		{
			var entity = new Entity("workflow", Guid.NewGuid());
			entity["name"] = name;
			entity["category"] = new OptionSetValue((int)category);
			entity["statecode"] = new OptionSetValue((int)state);
			if (clientData != null) entity["clientdata"] = clientData;
			return new Workflow(entity);
		}

		private void SetupByName(params Workflow[] workflows)
		{
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<string?>()))
				.ReturnsAsync(workflows);
		}


		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheFileDoesNotExist()
		{
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = Path.Combine(Path.GetTempPath(), "pacx-test-does-not-exist.json") };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheFileIsNotValidJson()
		{
			var file = WriteTempFile("this is not json");
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "valid json");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateTheClientData_AfterTheValidationPassed()
		{
			var workflow = CreateWorkflow();
			SetupByName(workflow);
			Entity? updated = null;
			this.OrganizationServiceMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => updated = Clone(e))
				.Returns(Task.CompletedTask);

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(updated, "The workflow must be updated.");
			StringAssert.Contains(updated.GetAttributeValue<string>("clientdata"), "\"changed\"");
			Assert.IsFalse(updated.GetAttributeValue<string>("clientdata").Contains('\n'), "The definition must be stored compacted.");
			this.validatorMock.Verify(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.Is<string>(c => c.Contains("\"changed\"")), It.IsAny<CancellationToken>()), Times.Once, "The definition must be validated before the flow is touched.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNotTouchTheFlow_WhenTheValidationFails()
		{
			SetupByName(CreateWorkflow());
			this.validatorMock
				.Setup(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(CommandResult.Fail("The flow engine rejected the definition: invalid."));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "rejected");
			this.OrganizationServiceMock.Verify(s => s.UpdateAsync(It.IsAny<Entity>()), Times.Never, "A rejected definition must leave the existing flow untouched.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFindTheWorkflowById()
		{
			var workflow = CreateWorkflow();
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), workflow.Id))
				.ReturnsAsync(workflow);

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Id = workflow.Id, DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(workflow.Id, result["workflowid"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoWorkflowIsFound()
		{
			SetupByName();

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No workflow found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenMoreThanOneWorkflowMatches()
		{
			SetupByName(
				CreateWorkflow(name: "My Flow 1"),
				CreateWorkflow(name: "My Flow 2"));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "My Flow 1");
			StringAssert.Contains(this.Output.ToString(), "My Flow 2");
			StringAssert.Contains(result.ErrorMessage, "--id");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPreferTheExactMatch_WhenNamesDifferOnlyBySpaces()
		{
			var exact = CreateWorkflow(name: " My Flow");
			SetupByName(
				exact,
				CreateWorkflow(name: "My Flow With Longer Name"));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(exact.Id, result["workflowid"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_ForClassicWorkflows()
		{
			SetupByName(CreateWorkflow(category: Workflow.Category.Worfklow, clientData: null));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not a modern flow");
			this.validatorMock.Verify(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldDoNothing_WhenTheDefinitionIsIdentical()
		{
			var compacted = "{\"properties\":{\"definition\":{\"actions\":{}}}}";
			var workflow = CreateWorkflow(clientData: compacted);
			SetupByName(workflow);

			var file = WriteTempFile("{\r\n  \"properties\": {\r\n    \"definition\": {\r\n      \"actions\": {}\r\n    }\r\n  }\r\n}");
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "nothing to update");
			Assert.AreEqual(workflow.Id, result["workflowid"], "The resolved id must be returned also when nothing changes.");
			this.validatorMock.Verify(v => v.ValidateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPassTheSolution_ToTheLookup()
		{
			SetupByName(CreateWorkflow());

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", SolutionName = "mysolution", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.workflowRepositoryMock.Verify(r => r.GetDefinitionByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "My Flow", "mysolution"));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldTellThatAnActivatedFlowChangesImmediately()
		{
			var workflow = CreateWorkflow(state: Workflow.State.Activated);
			SetupByName(workflow);
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), workflow.Id))
				.ReturnsAsync(CreateWorkflow(state: Workflow.State.Activated));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "effective immediately");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWarn_WhenTheFlowComesBackDeactivated()
		{
			var workflow = CreateWorkflow(state: Workflow.State.Activated);
			SetupByName(workflow);
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), workflow.Id))
				.ReturnsAsync(CreateWorkflow(state: Workflow.State.Draft));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "not anymore", "The user must be told when the update left the flow deactivated.");
		}
	}
}
