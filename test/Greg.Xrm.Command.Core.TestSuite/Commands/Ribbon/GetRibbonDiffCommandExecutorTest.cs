using System.Xml.Linq;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class GetRibbonDiffCommandExecutorTest : CommandExecutorTestBase
	{
		private static readonly string ExpectedXml = XElement.Parse("<RibbonDiffXml><OldAction /></RibbonDiffXml>").ToString();

		private readonly Mock<ISolutionRepository> solutionRepository = new();

		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public async Task WritesOnlyXmlToConsoleFromTheExportedSolution(string tableName)
		{
			var requests = RibbonDiffSolutionSetup.Setup(OrganizationServiceMock, solutionRepository, tableName);
			OrganizationServiceRepositoryMock.Setup(x => x.GetCurrentDefaultSolutionAsync()).ReturnsAsync(RibbonDiffSolutionSetup.SolutionName);
			var executor = new GetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

			var result = await executor.ExecuteAsync(new GetRibbonDiffCommand { TableName = tableName }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(ExpectedXml + Environment.NewLine, Output.ToString());
			Assert.AreEqual(RibbonDiffSolutionSetup.SolutionName, requests.OfType<ExportSolutionRequest>().Single().SolutionName);
			solutionRepository.Verify(x => x.CreateTemporarySolutionAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<EntityReference>()), Times.Never);
		}

		[TestMethod]
		public async Task WritesXmlToOutputFileAndReportsTheDefaultSolution()
		{
			var file = Path.Combine(Path.GetTempPath(), $"ribbon-{Guid.NewGuid():N}.xml");
			try
			{
				RibbonDiffSolutionSetup.Setup(OrganizationServiceMock, solutionRepository, "account");
				OrganizationServiceRepositoryMock.Setup(x => x.GetCurrentDefaultSolutionAsync()).ReturnsAsync(RibbonDiffSolutionSetup.SolutionName);
				var executor = new GetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

				var result = await executor.ExecuteAsync(new GetRibbonDiffCommand { TableName = "account", FileName = file }, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.AreEqual(ExpectedXml, await File.ReadAllTextAsync(file));
				StringAssert.Contains(Output.ToString(), "Using default solution <RibbonDiff>");
			}
			finally
			{
				File.Delete(file);
			}
		}

		[TestMethod]
		public async Task RejectsManagedSolutionBeforeExport()
		{
			var requests = RibbonDiffSolutionSetup.Setup(OrganizationServiceMock, solutionRepository, "account", isManaged: true);
			var executor = new GetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

			var result = await executor.ExecuteAsync(new GetRibbonDiffCommand
			{
				TableName = "account", SolutionName = RibbonDiffSolutionSetup.SolutionName
			}, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "is managed");
			Assert.IsFalse(requests.OfType<ExportSolutionRequest>().Any());
		}
	}
}
