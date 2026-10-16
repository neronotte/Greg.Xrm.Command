using System.IO.Compression;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[TestClass]
	public class TranslationImportCommandExecutorTest
	{
		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IOrganizationServiceAsync2> crmMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();

			repoMock
				.Setup(r => r.GetCurrentConnectionAsync())
				.ReturnsAsync(crmMock.Object);

			return (output, repoMock, crmMock);
		}

		private static void SetupImportResponse(Mock<IOrganizationServiceAsync2> crmMock, Action<ImportTranslationRequest> capture)
		{
			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					capture((ImportTranslationRequest)request);
					return new ImportTranslationResponse();
				});
		}

		private static void SetupCompletedImportJob(Mock<IOrganizationServiceAsync2> crmMock)
		{
			crmMock
				.Setup(c => c.RetrieveAsync(
					"importjob",
					It.IsAny<Guid>(),
					It.IsAny<ColumnSet>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync((string _, Guid id, ColumnSet _, CancellationToken _) =>
					new Entity("importjob", id)
					{
						["progress"] = 100d,
						["completedon"] = DateTime.UtcNow,
					});
		}

		private static string CreateZipFile(string root, string fileName)
		{
			var zipPath = Path.Combine(root, fileName);
			using var stream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
			using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
			var entry = archive.CreateEntry("customizations.xml");
			using var writer = new StreamWriter(entry.Open());
			writer.Write("<translation />");
			return zipPath;
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldImportZipFile()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			ImportTranslationRequest? capturedRequest = null;
			SetupImportResponse(crmMock, r => capturedRequest = r);
			SetupCompletedImportJob(crmMock);

			var root = Utility.CreateTempFolder();
			var zipPath = CreateZipFile(root, "translations.zip");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = zipPath },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Once);
				crmMock.Verify(c => c.ExecuteAsync(It.IsAny<ImportTranslationRequest>(), It.IsAny<CancellationToken>()), Times.Once);

				Assert.IsNotNull(capturedRequest);
				Assert.AreNotEqual(Guid.Empty, capturedRequest.ImportJobId);
				Assert.IsTrue(capturedRequest.TranslationFile.Length > 0);
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldRepackFolder_WhenRepackIsRequested()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			ImportTranslationRequest? capturedRequest = null;
			SetupImportResponse(crmMock, r => capturedRequest = r);
			SetupCompletedImportJob(crmMock);

			var root = Utility.CreateTempFolder();
			Utility.CreateFile(root, Path.Combine("translations", "customizations.xml"), "<translation />");
			var folder = Path.Combine(root, "translations");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = folder, Repack = true },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsNotNull(capturedRequest);
				Assert.IsTrue(capturedRequest.TranslationFile.Length > 0);

				using var archive = new ZipArchive(new MemoryStream(capturedRequest.TranslationFile), ZipArchiveMode.Read);
				Assert.IsTrue(archive.Entries.Any(x => x.Name == "customizations.xml"));
				Assert.IsTrue(File.Exists(Path.Combine(root, "translations.zip")));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPollImportJobUntilItCompletes()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			ImportTranslationRequest? capturedRequest = null;
			SetupImportResponse(crmMock, r => capturedRequest = r);

			var pollCount = 0;
			crmMock
				.Setup(c => c.RetrieveAsync(
					"importjob",
					It.IsAny<Guid>(),
					It.IsAny<ColumnSet>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync((string _, Guid id, ColumnSet _, CancellationToken _) =>
				{
					pollCount++;
					return new Entity("importjob", id)
					{
						["progress"] = pollCount == 1 ? 50d : 100d,
						["completedon"] = pollCount == 1 ? null : DateTime.UtcNow,
					};
				});

			var root = Utility.CreateTempFolder();
			var zipPath = CreateZipFile(root, "translations.zip");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = zipPath },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.AreEqual(2, pollCount);
				Assert.AreEqual(100d, result["Progress"]);
				StringAssert.Contains(output.ToString(), "50%");
				StringAssert.Contains(output.ToString(), "100%");
				Assert.IsNotNull(capturedRequest);
				crmMock.Verify(c => c.RetrieveAsync(
					"importjob",
					capturedRequest.ImportJobId,
					It.Is<ColumnSet>(columns =>
						columns.Columns.Contains("progress") &&
						columns.Columns.Contains("completedon")),
					It.IsAny<CancellationToken>()), Times.Exactly(2));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSkipPolling_WhenNoWaitIsRequested()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			ImportTranslationRequest? capturedRequest = null;
			SetupImportResponse(crmMock, r => capturedRequest = r);

			var root = Utility.CreateTempFolder();
			var zipPath = CreateZipFile(root, "translations.zip");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = zipPath, NoWait = true },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsNotNull(capturedRequest);
				Assert.AreEqual(capturedRequest.ImportJobId, result["ImportJobId"]);
				Assert.AreEqual(false, result["WaitedForCompletion"]);
				Assert.IsFalse(result.ContainsKey("Progress"));
				crmMock.Verify(c => c.RetrieveAsync(
					It.IsAny<string>(),
					It.IsAny<Guid>(),
					It.IsAny<ColumnSet>(),
					It.IsAny<CancellationToken>()), Times.Never);
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenFolderIsProvidedWithoutRepack()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			var root = Utility.CreateTempFolder();
			Utility.CreateFile(root, Path.Combine("translations", "customizations.xml"), "<translation />");
			var folder = Path.Combine(root, "translations");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = folder },
					CancellationToken.None);

				// Assert
				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "--repack");
				crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenFileDoesNotExist()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

			// Act
			var result = await executor.ExecuteAsync(
				new TranslationImportCommand { Path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip") },
				CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					"Simulated Dataverse fault"));

			var root = Utility.CreateTempFolder();
			var zipPath = CreateZipFile(root, "translations.zip");

			try
			{
				var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationImportCommand { Path = zipPath },
					CancellationToken.None);

				// Assert
				Assert.IsFalse(result.IsSuccess);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenPathIsMissing()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new TranslationImportCommandExecutor(output, repoMock.Object);

			// Act
			var result = await executor.ExecuteAsync(new TranslationImportCommand(), CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
