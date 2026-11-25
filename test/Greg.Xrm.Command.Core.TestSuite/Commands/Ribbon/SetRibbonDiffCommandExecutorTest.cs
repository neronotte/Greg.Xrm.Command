using System.Xml.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class SetRibbonDiffCommandExecutorTest : RibbonDiffCommandTestBase
	{
		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public async Task ExportsAndReimportsTheSelectedSolutionAndPublishesOnlyTheSelectedScope(string table)
		{
			using var cancellation = new CancellationTokenSource();
			var result = await NewSet().ExecuteAsync(SetCommand(table), cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(true, result["Changed"]);
			Assert.AreEqual(true, result["Published"]);
			Assert.AreEqual(SolutionName, Requests.OfType<ExportSolutionRequest>().Single().SolutionName);
			var import = Requests.OfType<ImportSolutionRequest>().Single();
			CollectionAssert.AreEqual(UpdatedZip, import.CustomizationFile);
			Assert.IsTrue(import.OverwriteUnmanagedCustomizations);
			Ribbons.Verify(a => a.Replace(OriginalZip, table, It.Is<XElement>(x => XNode.DeepEquals(x, Replacement))), Times.Once);
			Assert.AreEqual(Original.ToString(), await File.ReadAllTextAsync(BackupFile));
			ConsoleInput.VerifyNoOtherCalls();
			var publish = Requests.OfType<PublishXmlRequest>().Single();
			if (table.Length == 0)
			{
				Assert.AreEqual("<importexportxml><ribbons><ribbon></ribbon></ribbons></importexportxml>", publish.ParameterXml);
				Publisher.VerifyNoOtherCalls();
			}
			else
			{
				Publisher.Verify(p => p.AddTable("account"), Times.Once);
				StringAssert.Contains(publish.ParameterXml, "<entity>account</entity>");
			}
			Assert.IsFalse(Requests.OfType<PublishAllXmlRequest>().Any());
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<ImportSolutionRequest>(), cancellation.Token), Times.Once);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), cancellation.Token), Times.Once);
		}

		[TestMethod]
		public async Task UnchangedRibbonStillCreatesTheRequestedBackupWithoutImporting()
		{
			await File.WriteAllTextAsync(InputFile, Original.ToString());
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(false, result["Changed"]);
			Assert.AreEqual(false, result["Published"]);
			Assert.AreEqual(Original.ToString(), await File.ReadAllTextAsync(BackupFile));
			Ribbons.Verify(a => a.Replace(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<XElement>()), Times.Never);
			ConsoleInput.VerifyNoOtherCalls();
			VerifyNoImportOrPublish();
		}


		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task PromptsWithoutBackupAndHonorsTheAnswer(bool confirmed)
		{
			SetConsoleInput(confirmed ? "y\n" : "n\n");
			var result = await NewSet().ExecuteAsync(SetCommand(backup: null), CancellationToken.None);
			Assert.AreEqual(confirmed, result.IsSuccess, result.ErrorMessage);
			ConsoleInput.Verify(c => c.ReadKeyAsync(It.IsAny<bool>(), CancellationToken.None), Times.AtLeastOnce);
			Assert.IsFalse(File.Exists(BackupFile));
			if (!confirmed) VerifyNoImportOrPublish();
		}

		[TestMethod]
		public async Task EmptyConfirmationDefaultsToNo()
		{
			SetConsoleInput("\n");
			var result = await NewSet().ExecuteAsync(SetCommand(backup: null), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Aborted");
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		public async Task NonInteractiveInputWithoutBackupAborts()
		{
			Console.Object.Profile.Capabilities.Interactive = false;
			var result = await NewSet().ExecuteAsync(SetCommand(backup: null), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Aborted");
			ConsoleInput.VerifyNoOtherCalls();
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		public async Task UnchangedWithoutBackupNeedsNoConfirmation()
		{
			await File.WriteAllTextAsync(InputFile, Original.ToString());
			var result = await NewSet().ExecuteAsync(SetCommand(backup: null), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess);
			ConsoleInput.VerifyNoOtherCalls();
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		[DataRow("missing")]
		[DataRow("expanded")]
		[DataRow("malformed")]
		[DataRow("dtd")]
		public async Task InvalidInputFailsBeforeConnecting(string input)
		{
			if (input == "missing") File.Delete(InputFile);
			else await File.WriteAllTextAsync(InputFile, input switch
			{
				"expanded" => "<RibbonDefinitions/>",
				"malformed" => "<RibbonDiffXml>",
				_ => "<!DOCTYPE RibbonDiffXml [<!ENTITY value 'text'>]><RibbonDiffXml>&value;</RibbonDiffXml>"
			});
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			OrganizationServiceRepositoryMock.Verify(c => c.GetCurrentConnectionAsync(), Times.Never);
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ExistingBackupIsOverwrittenWithTheCurrentRibbon(bool unchanged)
		{
			if (unchanged) await File.WriteAllTextAsync(InputFile, Original.ToString());
			await File.WriteAllTextAsync(BackupFile, new string('x', 1000));
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(Original.ToString(), await File.ReadAllTextAsync(BackupFile));
			Assert.AreEqual(!unchanged, result["Changed"]);
			ConsoleInput.VerifyNoOtherCalls();
			if (unchanged) VerifyNoImportOrPublish();
			else Assert.AreEqual(1, Requests.OfType<ImportSolutionRequest>().Count());
		}

		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public async Task SchemaInvalidRibbonFailsLocallyBeforeConnectingOrWritingBackup(string table)
		{
			await File.WriteAllTextAsync(InputFile, "<RibbonDiffXml>\n  <UnknownElement/>\n</RibbonDiffXml>");
			await File.WriteAllTextAsync(BackupFile, "existing backup");
			var result = await NewSet().ExecuteAsync(SetCommand(table), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Invalid RibbonDiffXml at line 2");
			Assert.AreEqual("existing backup", await File.ReadAllTextAsync(BackupFile));
			OrganizationServiceRepositoryMock.Verify(c => c.GetCurrentConnectionAsync(), Times.Never);
			ConsoleInput.VerifyNoOtherCalls();
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		public async Task MissingBackupDirectoryFailsBeforeConnecting()
		{
			var command = SetCommand(); command.BackupFile = Path.Combine(TempDirectory, "missing", "backup.xml");
			var result = await NewSet().ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			OrganizationServiceRepositoryMock.Verify(c => c.GetCurrentConnectionAsync(), Times.Never);
			VerifyNoImportOrPublish();
		}

		[TestMethod]
		public async Task MissingDefaultSolutionFailsBeforeExport()
		{
			OrganizationServiceRepositoryMock.Setup(c => c.GetCurrentDefaultSolutionAsync()).ReturnsAsync((string?)null);
			var command = SetCommand(); command.SolutionName = null;
			var result = await NewSet().ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, Requests.Count);
		}

		[TestMethod]
		public async Task UsesAndReportsTheDefaultSolution()
		{
			var command = SetCommand(); command.SolutionName = null;
			var result = await NewSet().ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(Output.ToString(), "Using default solution <RibbonDiff>");
		}

		[TestMethod]
		public async Task ManagedSolutionFailsBeforeExport()
		{
			SetSolution(managed: true);
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, Requests.Count);
		}

		[TestMethod]
		public async Task InvalidSolutionFailsBeforeExport()
		{
			Ribbons.Setup(r => r.ValidateSolutionAsync(OrganizationServiceMock.Object, SolutionId, SolutionName, ComponentId, It.IsAny<ComponentType>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync("Unsafe solution");
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, Requests.Count);
			Assert.IsFalse(File.Exists(BackupFile));
		}

		[TestMethod]
		public async Task ImportFaultReturnsFailureAndDoesNotPublish()
		{
			OrganizationServiceMock.Setup(c => c.ExecuteAsync(It.IsAny<ImportSolutionRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Import failed"));
			var result = await NewSet().ExecuteAsync(SetCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Import failed");
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task CancellationPropagatesWithoutPublishing()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => NewSet().ExecuteAsync(SetCommand(), cancellation.Token));
			VerifyNoImportOrPublish();
		}
	}
}
