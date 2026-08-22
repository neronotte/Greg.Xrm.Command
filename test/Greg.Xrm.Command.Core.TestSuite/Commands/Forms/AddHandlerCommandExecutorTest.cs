using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Forms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class AddHandlerCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly AddHandlerCommandExecutor executor;
		private readonly Mock<IFormRepository> formRepositoryMock = new();
		private readonly Mock<ISolutionRepository> solutionRepositoryMock = new();
		private readonly IFormWrapperFactory formWrapperFactory = new FormWrapperFactory();
		private QueryExpression? capturedQuery;

		public AddHandlerCommandExecutorTest()
		{
			this.executor = new AddHandlerCommandExecutor(
				this.OrganizationServiceRepositoryMock.Object,
				this.Output,
				this.formRepositoryMock.Object,
				this.solutionRepositoryMock.Object,
				formWrapperFactory);
		}

		// ── helpers ───────────────────────────────────────────────────────────

		private void SetupWebResourceResponse(params Entity[] rows)
		{
			this.OrganizationServiceMock
				.Setup(s => s.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.Callback<QueryBase>(qb => this.capturedQuery = qb as QueryExpression)
				.ReturnsAsync(new EntityCollection(rows.ToList()));
		}

		private static Form CreateForm(string name)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = name;
			entity["formxml"] = "<form><tabs /></form>";
			return new Form(entity);
		}

		private static AddHandlerCommand CreateCommand() => new()
		{
			TableName = "account",
			Library = "myprefix_scripts.js",
			Function = "My.Account.onLoad"
		};

		// ── --output validation ───────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenOutputDirectoryDoesNotExist()
		{
			var command = CreateCommand();
			command.TempDir = "C:\\this\\folder\\does\\not\\exist\\at\\all";

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		// ── webresource type check ────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldRequireJavascriptWebresource()
		{
			SetupWebResourceResponse();
			var command = CreateCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "javascript");

			Assert.IsNotNull(this.capturedQuery);
			var typeCondition = this.capturedQuery.Criteria.Conditions
				.SingleOrDefault(c => c.AttributeName == "webresourcetype");
			Assert.IsNotNull(typeCondition, "The webresource lookup must filter on the resource type.");
			Assert.AreEqual((int)WebResourceType.Script, typeCondition.Values[0]);
		}

		// ── form resolution ───────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenFormNameDoesNotMatchTheOnlyForm()
		{
			SetupWebResourceResponse(new Entity("webresource", Guid.NewGuid()));
			this.formRepositoryMock
				.Setup(r => r.GetMainFormByTableNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), "account"))
				.ReturnsAsync([CreateForm("Information")]);

			var command = CreateCommand();
			command.FormName = "DoesNotExist";

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldAcceptMatchingFormName_WhenTableHasOneForm()
		{
			SetupWebResourceResponse(new Entity("webresource", Guid.NewGuid()));
			this.formRepositoryMock
				.Setup(r => r.GetMainFormByTableNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), "account"))
				.ReturnsAsync([CreateForm("Information")]);
			this.solutionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync((Greg.Xrm.Command.Model.Solution?)null);

			var command = CreateCommand();
			command.FormName = "information";
			command.SolutionName = "SomeSolution";

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// the run fails later at the solution lookup, which proves the
			// case-insensitive form name match was accepted
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "SomeSolution");
		}
	}
}
