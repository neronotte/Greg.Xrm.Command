using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class RemoveHandlerCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly RemoveHandlerCommandExecutor executor;
		private readonly Mock<IFormRepository> formRepositoryMock = new();
		private readonly Mock<ISolutionRepository> solutionRepositoryMock = new();
		private Entity? updatedEntity;

		public RemoveHandlerCommandExecutorTest()
		{
			this.executor = new RemoveHandlerCommandExecutor(
				this.OrganizationServiceRepositoryMock.Object,
				this.Output,
				this.formRepositoryMock.Object,
				this.solutionRepositoryMock.Object);

			this.OrganizationServiceMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()))
				.Callback<Entity, CancellationToken>((e, _) => this.updatedEntity = e)
				.Returns(Task.CompletedTask);

			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new OrganizationResponse());
		}

		private const string FormXmlWithHandler =
			"<form><tabs /><events><event name=\"onload\" application=\"false\" active=\"false\">" +
			"<Handlers><Handler functionName=\"My.Account.onLoad\" libraryName=\"myprefix_scripts.js\" " +
			"handlerUniqueId=\"{11111111-1111-1111-1111-111111111111}\" enabled=\"true\" parameters=\"\" " +
			"passExecutionContext=\"true\" /></Handlers></event></events>" +
			"<formLibraries><Library name=\"myprefix_scripts.js\" libraryUniqueId=\"{22222222-2222-2222-2222-222222222222}\" /></formLibraries></form>";

		private void SetupForm(string formXml)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = "Information";
			entity["formxml"] = formXml;
			this.formRepositoryMock
				.Setup(r => r.GetMainFormByTableNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), "account"))
				.ReturnsAsync([new Form(entity)]);
		}

		private static RemoveHandlerCommand CreateCommand() => new()
		{
			TableName = "account",
			Library = "myprefix_scripts.js",
			Function = "My.Account.onLoad",
			Fast = true
		};

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenOutputDirectoryDoesNotExist()
		{
			var command = CreateCommand();
			command.TempDir = "C:\\this\\folder\\does\\not\\exist\\at\\all";

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldMakeNoChange_WhenHandlerIsNotRegistered()
		{
			SetupForm("<form><tabs /></form>");
			var command = CreateCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNull(this.updatedEntity, "Nothing must be written when the handler is not registered.");
			StringAssert.Contains(this.Output.ToString(), "not registered");
		}

		private const string FormXmlWithHandlerOnTwoEvents =
			"<form><tabs /><events><event name=\"onload\" application=\"false\" active=\"false\">" +
			"<Handlers><Handler functionName=\"My.Account.onLoad\" libraryName=\"myprefix_scripts.js\" " +
			"handlerUniqueId=\"{11111111-1111-1111-1111-111111111111}\" enabled=\"true\" parameters=\"\" " +
			"passExecutionContext=\"true\" /></Handlers></event>" +
			"<event name=\"onsave\" application=\"false\" active=\"false\">" +
			"<Handlers><Handler functionName=\"My.Account.onLoad\" libraryName=\"myprefix_scripts.js\" " +
			"handlerUniqueId=\"{33333333-3333-3333-3333-333333333333}\" enabled=\"true\" parameters=\"\" " +
			"passExecutionContext=\"true\" /></Handlers></event></events>" +
			"<formLibraries><Library name=\"myprefix_scripts.js\" libraryUniqueId=\"{22222222-2222-2222-2222-222222222222}\" /></formLibraries></form>";

		[TestMethod]
		public async Task ExecuteAsync_ShouldKeepLibrary_WhenStillReferencedByAnotherEvent()
		{
			SetupForm(FormXmlWithHandlerOnTwoEvents);
			var command = CreateCommand();
			command.Event = FormEvent.OnSave;

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(this.updatedEntity);
			var newFormXml = (string)this.updatedEntity["formxml"];
			Assert.IsFalse(newFormXml.Contains("onsave"), "The onsave registration must be removed.");
			Assert.IsTrue(newFormXml.Contains("\"onload\""), "The onload registration must survive.");
			Assert.IsTrue(newFormXml.Contains("formLibraries"), "The library must stay while another event references it.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldRemoveHandlerAndUnusedLibrary_InFastMode()
		{
			SetupForm(FormXmlWithHandler);
			var command = CreateCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(this.updatedEntity);
			var newFormXml = (string)this.updatedEntity["formxml"];
			Assert.IsFalse(newFormXml.Contains("My.Account.onLoad"), "The handler must be removed.");
			Assert.IsFalse(newFormXml.Contains("myprefix_scripts.js"), "The unreferenced library must be removed as well.");
		}
	}
}
