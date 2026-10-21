using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class GetRibbonDiffCommandExecutorTest : RibbonDiffCommandTestBase
	{
		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public async Task WritesOnlyXmlToConsoleFromTheExportedSolution(string tableName)
		{
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { TableName = tableName }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(Original + Environment.NewLine, Output.ToString());
			Assert.AreEqual(0, result.Count, "Result metadata would be printed after the XML by the command runner.");
			Assert.AreEqual(SolutionName, Requests.OfType<ExportSolutionRequest>().Single().SolutionName);
			Ribbons.Verify(a => a.Read(OriginalZip, tableName), Times.Once);
			Assert.IsFalse(File.Exists(OutputFile));
			Solutions.Verify(s => s.CreateTemporarySolutionAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<EntityReference>()), Times.Never);
		}

		[TestMethod]
		public async Task WritesXmlToOutputFileAndReportsTheDefaultSolution()
		{
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { TableName = "account", FileName = OutputFile }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(Original.ToString(), await File.ReadAllTextAsync(OutputFile));
			Assert.AreEqual(SolutionName, result["SolutionName"]);
			Assert.AreEqual(OutputFile, result["OutputFile"]);
			StringAssert.Contains(Output.ToString(), "Using default solution <RibbonDiff>");
			StringAssert.Contains(Output.ToString(), "Connecting to the current dataverse environment...");
		}

		[TestMethod]
		public async Task RejectsManagedSolutionBeforeExport()
		{
			SetSolution(managed: true);
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { TableName = "account", SolutionName = SolutionName }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "is managed");
			Assert.AreEqual(0, Requests.Count);
		}

		[TestMethod]
		public async Task RejectsMissingDefaultSolutionBeforeExport()
		{
			OrganizationServiceRepositoryMock.Setup(c => c.GetCurrentDefaultSolutionAsync()).ReturnsAsync((string?)null);
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, Requests.Count);
		}

		[TestMethod]
		public async Task RejectsInvalidSolutionBeforeExport()
		{
			Ribbons.Setup(r => r.ValidateSolutionAsync(OrganizationServiceMock.Object, SolutionId, SolutionName, ComponentId, ComponentType.Entity, It.IsAny<CancellationToken>()))
				.ReturnsAsync("Unsupported solution components");
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { TableName = "account" }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unsupported solution components");
			Assert.AreEqual(0, Requests.Count);
		}

		[TestMethod]
		public async Task InvalidOutputPathFailsBeforeConnecting()
		{
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { FileName = Path.Combine(TempDirectory, "missing", "output.xml") }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			OrganizationServiceRepositoryMock.Verify(c => c.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task DataverseFaultReturnsFailureWithoutSaving()
		{
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<ExportSolutionRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Export failed"));
			var result = await NewGet().ExecuteAsync(new GetRibbonDiffCommand { FileName = OutputFile }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Export failed");
			Assert.IsFalse(File.Exists(OutputFile));
		}

		[TestMethod]
		public async Task CancellationPropagatesToTheCaller()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<ExportSolutionRequest>(), cancellation.Token)).ThrowsAsync(new OperationCanceledException(cancellation.Token));
			await Assert.ThrowsAsync<OperationCanceledException>(() => NewGet().ExecuteAsync(new GetRibbonDiffCommand(), cancellation.Token));
		}
	}
}
