using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class CleanCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CleanCommandExecutor executor;
		private readonly Mock<IFormRepository> formRepositoryMock = new();
		private readonly Mock<ISolutionRepository> solutionRepositoryMock = new();

		public CleanCommandExecutorTest()
		{
			this.executor = new CleanCommandExecutor(
				this.OrganizationServiceRepositoryMock.Object,
				this.Output,
				this.formRepositoryMock.Object,
				this.solutionRepositoryMock.Object);
		}

		private static Form CreateForm(string name)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = name;
			entity["formxml"] = "<form><tabs /></form>";
			return new Form(entity);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenOutputDirectoryDoesNotExist()
		{
			var command = new CleanCommand
			{
				TableName = "account",
				TempDir = "C:\\this\\folder\\does\\not\\exist\\at\\all"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenFormNameDoesNotMatchTheOnlyForm()
		{
			this.formRepositoryMock
				.Setup(r => r.GetMainFormByTableNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), "account"))
				.ReturnsAsync([CreateForm("Information")]);

			var command = new CleanCommand
			{
				TableName = "account",
				FormName = "DoesNotExist"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not found");
		}
	}
}
