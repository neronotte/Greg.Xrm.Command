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

		private readonly List<string> tempFiles = [];

		public UpdateCommandExecutorTest()
		{
			this.executor = new UpdateCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.workflowRepositoryMock.Object);
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
		public async Task ExecuteAsync_ShouldUpdateTheClientData()
		{
			SetupByName(CreateWorkflow());
			Entity? updated = null;
			this.OrganizationServiceMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e =>
				{
					updated = new Entity(e.LogicalName, e.Id);
					foreach (var a in e.Attributes) updated[a.Key] = a.Value;
				})
				.Returns(Task.CompletedTask);

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(updated, "The workflow must be updated.");
			StringAssert.Contains(updated.GetAttributeValue<string>("clientdata"), "\"changed\"");
			Assert.IsFalse(updated.GetAttributeValue<string>("clientdata").Contains('\n'), "The definition must be stored compacted.");
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
			this.OrganizationServiceMock.Verify(s => s.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldDoNothing_WhenTheDefinitionIsIdentical()
		{
			var compacted = "{\"properties\":{\"definition\":{\"actions\":{}}}}";
			SetupByName(CreateWorkflow(clientData: compacted));

			var file = WriteTempFile("{\r\n  \"properties\": {\r\n    \"definition\": {\r\n      \"actions\": {}\r\n    }\r\n  }\r\n}");
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "nothing to update");
			this.OrganizationServiceMock.Verify(s => s.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldTellThatAnActivatedFlowChangesImmediately()
		{
			SetupByName(CreateWorkflow(state: Workflow.State.Activated));

			var file = WriteTempFile(NewDefinition);
			var command = new UpdateCommand { Name = "My Flow", DefinitionFile = file };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "effective immediately");
		}
	}
}
