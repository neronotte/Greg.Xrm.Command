using System.IO.Compression;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[TestClass]
	public class TranslationExportCommandExecutorTest
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

		private static void SetupExportResponse(Mock<IOrganizationServiceAsync2> crmMock, byte[] payload, Action<ExportTranslationRequest> capture)
		{
			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					capture((ExportTranslationRequest)request);
					var response = new ExportTranslationResponse();
					response.Results["ExportTranslationFile"] = payload;
					return response;
				});
		}

		private static byte[] CreateZipBytes(params string[] entryNames)
		{
			using var ms = new MemoryStream();
			using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
			{
				foreach (var entryName in entryNames)
				{
					var entry = archive.CreateEntry(entryName);
					using var writer = new StreamWriter(entry.Open());
					writer.Write("<translation />");
				}
			}
			return ms.ToArray();
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldExportTranslationPackage()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			ExportTranslationRequest? capturedRequest = null;
			SetupExportResponse(crmMock, CreateZipBytes("customizations.xml"), r => capturedRequest = r);

			var root = Utility.CreateTempFolder();
			var filePath = Path.Combine(root, "export.zip");

			try
			{
				var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationExportCommand { SolutionName = "MySolution", Path = filePath },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Once);
				crmMock.Verify(c => c.ExecuteAsync(It.IsAny<ExportTranslationRequest>(), It.IsAny<CancellationToken>()), Times.Once);

				Assert.IsNotNull(capturedRequest);
				Assert.AreEqual("MySolution", capturedRequest.SolutionName);
				Assert.IsTrue(File.Exists(filePath));
				Assert.IsFalse(Directory.Exists(Path.Combine(root, "export")));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUnpackArchive_WhenUnpackIsRequested()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			SetupExportResponse(crmMock, CreateZipBytes("customizations.xml", "[Content_Types].xml"), _ => { });

			var root = Utility.CreateTempFolder();
			var filePath = Path.Combine(root, "export.zip");
			var expectedUnpackFolder = Path.Combine(root, "export");

			try
			{
				var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationExportCommand { SolutionName = "MySolution", Path = filePath, Unpack = true },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsTrue(File.Exists(filePath));
				Assert.IsTrue(Directory.Exists(expectedUnpackFolder));
				Assert.IsTrue(File.Exists(Path.Combine(expectedUnpackFolder, "customizations.xml")));
				Assert.IsTrue(File.Exists(Path.Combine(expectedUnpackFolder, "[Content_Types].xml")));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTranslationPackageIsEmpty()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			SetupExportResponse(crmMock, [], _ => { });

			var root = Utility.CreateTempFolder();
			var filePath = Path.Combine(root, "export.zip");

			try
			{
				var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationExportCommand { SolutionName = "MySolution", Path = filePath },
					CancellationToken.None);

				// Assert
				Assert.IsFalse(result.IsSuccess);
				Assert.IsFalse(File.Exists(filePath));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
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
			var filePath = Path.Combine(root, "export.zip");

			try
			{
				var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

				// Act
				var result = await executor.ExecuteAsync(
					new TranslationExportCommand { SolutionName = "MySolution", Path = filePath },
					CancellationToken.None);

				// Assert
				Assert.IsFalse(result.IsSuccess);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
				Assert.IsFalse(File.Exists(filePath));
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUseDefaultSolution_WhenSolutionNameIsMissing()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			repoMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync("DefaultSolution");
			ExportTranslationRequest? capturedRequest = null;
			SetupExportResponse(crmMock, CreateZipBytes("customizations.xml"), r => capturedRequest = r);
			var root = Utility.CreateTempFolder();
			var filePath = Path.Combine(root, "export.zip");
			var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

			try
			{
				// Act
				var result = await executor.ExecuteAsync(
					new TranslationExportCommand { Path = filePath },
					CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsNotNull(capturedRequest);
				Assert.AreEqual("DefaultSolution", capturedRequest.SolutionName);
				Assert.AreEqual("DefaultSolution", result["SolutionName"]);
				repoMock.Verify(r => r.GetCurrentDefaultSolutionAsync(), Times.Once);
			}
			finally
			{
				if (Directory.Exists(root)) Utility.DeleteFolder(root);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoSolutionNameOrDefaultExists()
		{
			// Arrange
			var (output, repoMock, crmMock) = CreateMocks();
			repoMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync((string?)null);
			var executor = new TranslationExportCommandExecutor(output, repoMock.Object);

			// Act
			var result = await executor.ExecuteAsync(new TranslationExportCommand(), CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			repoMock.Verify(r => r.GetCurrentDefaultSolutionAsync(), Times.Once);
			repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Never);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
