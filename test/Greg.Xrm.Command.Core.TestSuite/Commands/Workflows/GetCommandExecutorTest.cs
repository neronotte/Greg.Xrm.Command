using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class GetCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly GetCommandExecutor executor;
		private readonly Mock<IWorkflowRepository> workflowRepositoryMock = new();

		public GetCommandExecutorTest()
		{
			this.executor = new GetCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.workflowRepositoryMock.Object);
		}

		private const string FlowDefinition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}}}}}";

		private static Workflow CreateWorkflow(Workflow.Category category, string? clientData, string? xaml, string name = "My Flow")
		{
			var entity = new Entity("workflow", Guid.NewGuid());
			entity["name"] = name;
			entity["category"] = new OptionSetValue((int)category);
			if (clientData != null) entity["clientdata"] = clientData;
			if (xaml != null) entity["xaml"] = xaml;
			return new Workflow(entity);
		}

		private void SetupWorkflows(params Workflow[] workflows)
		{
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<string?>()))
				.ReturnsAsync(workflows);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReturnTheFlowDefinition_Indented()
		{
			SetupWorkflows(CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var written = this.Output.ToString();
			StringAssert.Contains(written, "\"triggers\"", "The definition must be returned.");
			StringAssert.Contains(written, Environment.NewLine + "  \"properties\"", "The json must be indented to be readable.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldKeepApostrophesAndUmlautsReadable()
		{
			SetupWorkflows(CreateWorkflow(Workflow.Category.ModernFlow, "{\"text\":\"Grün, it's fine\"}", null));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "Grün, it's fine", "Escaping the definition would make it hard to read and to diff.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReturnTheXaml_ForClassicWorkflows()
		{
			SetupWorkflows(CreateWorkflow(Workflow.Category.Worfklow, null, "<Activity>test</Activity>"));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "<Activity>test</Activity>");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoWorkflowIsFound()
		{
			SetupWorkflows();

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No workflow found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheNameIsNotUnique()
		{
			SetupWorkflows(
				CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null),
				CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "--solution");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFindTheWorkflow_WhenTheStoredNameHasSurroundingSpaces()
		{
			// happens with names that have been typed into the maker portal
			SetupWorkflows(CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null, " My Flow"));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "\"triggers\"");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPreferTheExactMatch_OverALongerName()
		{
			SetupWorkflows(
				CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null, "My Flow"),
				CreateWorkflow(Workflow.Category.ModernFlow, "{\"other\":true}", null, "My Flow v2"));

			var result = await executor.ExecuteAsync(new GetCommand { Name = "My Flow" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "\"triggers\"");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReturnTheDefinition_WhenSearchingById()
		{
			var id = Guid.NewGuid();
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), id))
				.ReturnsAsync(CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null));

			var result = await executor.ExecuteAsync(new GetCommand { Id = id }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "\"triggers\"");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheIdDoesNotExist()
		{
			this.workflowRepositoryMock
				.Setup(r => r.GetDefinitionByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>()))
				.ReturnsAsync((Workflow?)null);

			var result = await executor.ExecuteAsync(new GetCommand { Id = Guid.NewGuid() }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No workflow found with id");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheFileCannotBeWritten()
		{
			SetupWorkflows(CreateWorkflow(Workflow.Category.ModernFlow, FlowDefinition, null));

			// a directory as target makes the write fail deterministically
			var command = new GetCommand
			{
				Name = "My Flow",
				OutputFile = Path.GetTempPath()
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to write");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTheOutputFolderDoesNotExist()
		{
			var command = new GetCommand
			{
				Name = "My Flow",
				OutputFile = "C:\\this\\folder\\does\\not\\exist\\at\\all\\myflow.json"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}
	}
}
