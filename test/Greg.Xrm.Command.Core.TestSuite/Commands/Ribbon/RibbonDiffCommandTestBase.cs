using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Ribbon;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public abstract class RibbonDiffCommandTestBase : CommandExecutorTestBase
	{
		protected const string SolutionName = "RibbonDiff";
		protected readonly Guid SolutionId = Guid.NewGuid();
		protected readonly Guid ComponentId = Guid.NewGuid();
		protected readonly Mock<ISolutionRepository> Solutions = new();
		protected readonly Mock<IRibbonDiffService> Ribbons = new();
		protected readonly Mock<IAnsiConsole> Console = new();
		protected readonly Mock<IAnsiConsoleInput> ConsoleInput = new();
		protected readonly string TempDirectory = Directory.CreateTempSubdirectory("pacx-ribbon-").FullName;
		protected string InputFile => Path.Combine(TempDirectory, "input.xml");
		protected string BackupFile => Path.Combine(TempDirectory, "backup.xml");
		protected string OutputFile => Path.Combine(TempDirectory, "output.xml");
		protected readonly Mock<IPublishXmlBuilder> Publisher = new();
		protected readonly List<OrganizationRequest> Requests = [];
		protected readonly byte[] OriginalZip = [1, 2, 3];
		protected readonly byte[] UpdatedZip = [4, 5, 6];
		protected readonly XElement Original = XElement.Parse("<RibbonDiffXml><CustomActions><HideCustomAction HideActionId='old' Location='Mscrm.HomepageGrid.account.MainTab' /></CustomActions></RibbonDiffXml>");
		protected readonly XElement Replacement = XElement.Parse("<RibbonDiffXml><CustomActions><HideCustomAction HideActionId='new' Location='Mscrm.HomepageGrid.account.MainTab' /></CustomActions></RibbonDiffXml>");

		private sealed class TestSolution(Entity entity) : Greg.Xrm.Command.Model.Solution(entity);

		protected RibbonDiffCommandTestBase()
		{
			SetSolution();
			OrganizationServiceRepositoryMock.Setup(r => r.GetCurrentDefaultSolutionAsync()).ReturnsAsync(SolutionName);
			Ribbons.Setup(r => r.ResolveComponentAsync(OrganizationServiceMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 _, string table, CancellationToken _) =>
					(ComponentId, string.IsNullOrWhiteSpace(table) ? ComponentType.RibbonCustomization : ComponentType.Entity));
			Ribbons.Setup(r => r.ValidateSolutionAsync(OrganizationServiceMock.Object, SolutionId, SolutionName, ComponentId,
				It.IsAny<ComponentType>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
			Ribbons.Setup(r => r.ValidateXml(It.IsAny<XElement>(), It.IsAny<string>()))
				.Callback<XElement, string>(new RibbonDiffService(new Mock<IRibbonComponentRepository>().Object).ValidateXml);
			Ribbons.Setup(a => a.Read(OriginalZip, It.IsAny<string>())).Returns(() => new XElement(Original));
			Ribbons.Setup(a => a.Replace(OriginalZip, It.IsAny<string>(), It.IsAny<XElement>())).Returns(UpdatedZip);
			File.WriteAllText(InputFile, Replacement.ToString());
			var renderer = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(new StringWriter()) });
			renderer.Profile.Capabilities.Interactive = true;
			Console.SetupGet(c => c.Profile).Returns(renderer.Profile);
			Console.SetupGet(c => c.Input).Returns(ConsoleInput.Object);
			Console.SetupGet(c => c.Cursor).Returns(renderer.Cursor);
			Console.SetupGet(c => c.Pipeline).Returns(renderer.Pipeline);
			Console.SetupGet(c => c.ExclusivityMode).Returns(renderer.ExclusivityMode);
			Console.Setup(c => c.Write(It.IsAny<IRenderable>())).Callback<IRenderable>(renderer.Write);
			SetConsoleInput("y\n");
			Publisher.Setup(p => p.Build()).Returns(new PublishXmlRequest { ParameterXml = "<importexportxml><entities><entity>account</entity></entities></importexportxml>" });
			var exported = new ExportSolutionResponse();
			exported.Results["ExportSolutionFile"] = OriginalZip;
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Callback<OrganizationRequest, CancellationToken>((request, _) => Requests.Add(request))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) => request is ExportSolutionRequest ? exported : new OrganizationResponse());
		}

		protected void SetSolution(bool managed = false)
		{
			var entity = new Entity("solution", SolutionId) { ["uniquename"] = SolutionName, ["ismanaged"] = managed };
			Solutions.Setup(s => s.GetByUniqueNameAsync(OrganizationServiceMock.Object, SolutionName)).ReturnsAsync(new TestSolution(entity));
		}

		protected GetRibbonDiffCommandExecutor NewGet() => new(Output, OrganizationServiceRepositoryMock.Object, Solutions.Object, Ribbons.Object);
		protected SetRibbonDiffCommandExecutor NewSet() => new(Output, OrganizationServiceRepositoryMock.Object, Solutions.Object, Ribbons.Object, Console.Object, Publisher.Object);
		protected SetRibbonDiffCommand SetCommand(string table = "account", string? backup = "backup.xml") =>
			new() { FileName = InputFile, TableName = table, SolutionName = SolutionName, BackupFile = backup == null ? null : Path.Combine(TempDirectory, backup) };

		[TestCleanup]
		public void CleanupFiles() => Directory.Delete(TempDirectory, true);

		protected void SetConsoleInput(string input)
		{
			var keys = new Queue<ConsoleKeyInfo>(input.Select(c => c == '\n'
				? new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)
				: new ConsoleKeyInfo(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false)));
			ConsoleInput.Setup(c => c.ReadKeyAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => keys.Count == 0 ? (ConsoleKeyInfo?)null : keys.Dequeue());
		}

		protected void VerifyNoImportOrPublish()
		{
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<ImportSolutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishAllXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
