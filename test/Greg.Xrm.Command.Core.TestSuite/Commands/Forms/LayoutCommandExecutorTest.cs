using Greg.Xrm.Command.Services.Forms;
using Greg.Xrm.Command.Commands.Forms.Model;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class LayoutCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly Mock<IFormRepository> formRepository = new();

		[TestMethod]
		public async Task UsesInjectedRendererForSelectedForm()
		{
			var form = CreateForm("Information", "<form><tabs /></form>");
			var repository = new Mock<IFormRepository>();
			repository.Setup(service => service.GetByNameAsync(OrganizationServiceMock.Object, "account", "", true))
				.ReturnsAsync((CommandResult.Success(), form));
			var renderer = new Mock<IFormLayoutRenderer>();
			renderer.Setup(service => service.Render(It.IsAny<XElement>(), LayoutDisplay.Names, "Information"))
				.Returns(["rendered layout"]);
			var executor = new LayoutCommandExecutor(OrganizationServiceRepositoryMock.Object, repository.Object, renderer.Object, Output);

			var result = await executor.ExecuteAsync(new LayoutCommand { TableName = "account", Display = LayoutDisplay.Names }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			repository.Verify(service => service.GetByNameAsync(OrganizationServiceMock.Object, "account", "", true), Times.Once);
			renderer.Verify(service => service.Render(It.Is<XElement>(xml => xml.Name == "form" && xml.Element("tabs") != null), LayoutDisplay.Names, "Information"), Times.Once);
			StringAssert.Contains(Output.ToString(), "rendered layout");
			StringAssert.Contains(Output.ToString(), "Connecting to the current dataverse environment...");
		}

		[TestMethod]
		public async Task PrintsSelectedFormLayout()
		{
			var second = CreateForm("Second", "<form><tabs><tab name=\"chosen\" /></tabs></form>");
			formRepository.Setup(repo => repo.GetByNameAsync(OrganizationServiceMock.Object, "account", "Second", true))
				.ReturnsAsync((CommandResult.Success(), second));

			var executor = new LayoutCommandExecutor(OrganizationServiceRepositoryMock.Object, formRepository.Object, new FormLayoutRenderer(), Output);
			var result = await executor.ExecuteAsync(new LayoutCommand { TableName = "account", FormName = "Second" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(Output.ToString(), "Form Second" + Environment.NewLine);
			StringAssert.Contains(Output.ToString(), "Tab chosen [tab=1]" + Environment.NewLine);
			Assert.IsFalse(Output.ToString().Contains("(visible)"));
		}

		[TestMethod]
		public async Task RequiresFormNameWhenSeveralMainFormsExist()
		{
			formRepository.Setup(repo => repo.GetByNameAsync(OrganizationServiceMock.Object, "account", "", true))
				.ReturnsAsync((CommandResult.Fail("Please specify the form name using the --form parameter."), (Form?)null));

			var executor = new LayoutCommandExecutor(OrganizationServiceRepositoryMock.Object, formRepository.Object, new FormLayoutRenderer(), Output);
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
