using Greg.Xrm.Command.Commands.Forms.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class LayoutCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly Mock<IFormRepository> formRepository = new();

		[TestMethod]
		public async Task PrintsSelectedFormLayout()
		{
			var first = CreateForm("First", "<form><tabs /></form>");
			var second = CreateForm("Second", "<form><tabs><tab name=\"chosen\" /></tabs></form>");
			formRepository.Setup(repo => repo.GetMainFormByTableNameAsync(OrganizationServiceMock.Object, "account"))
				.ReturnsAsync([first, second]);

			var executor = new LayoutCommandExecutor(OrganizationServiceRepositoryMock.Object, formRepository.Object, Output);
			var result = await executor.ExecuteAsync(new LayoutCommand { TableName = "account", FormName = "Second" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(Output.ToString(), "Form Second (visible)");
			StringAssert.Contains(Output.ToString(), "Tab chosen [tab=1] (visible)");
		}

		[TestMethod]
		public async Task RequiresFormNameWhenSeveralMainFormsExist()
		{
			formRepository.Setup(repo => repo.GetMainFormByTableNameAsync(OrganizationServiceMock.Object, "account"))
				.ReturnsAsync([CreateForm("First", "<form />"), CreateForm("Second", "<form />")]);

			var executor = new LayoutCommandExecutor(OrganizationServiceRepositoryMock.Object, formRepository.Object, Output);
			var result = await executor.ExecuteAsync(new LayoutCommand { TableName = "account" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "--form");
		}

		private static Form CreateForm(string name, string formXml)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = name;
			entity["formxml"] = formXml;
			return new Form(entity);
		}
	}
}
