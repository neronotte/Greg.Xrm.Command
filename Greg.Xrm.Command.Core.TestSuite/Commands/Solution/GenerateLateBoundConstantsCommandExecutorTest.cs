using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.IO;
using Greg.Xrm.Command.Commands.Solution.Service;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GenerateCommandExecutorTest
	{
		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IOrganizationServiceAsync2> crmMock,
			Mock<IConstantsGeneratorService> serviceMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();
			var serviceMock = new Mock<IConstantsGeneratorService>();

			repoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(crmMock.Object);

			return (output, repoMock, crmMock, serviceMock);
		}

		// ── Solution resolution ────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_UsesDefaultSolutionWhenNotProvided()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			repoMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync("DefaultSolution");

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((5, 0));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = null,
				OutputCs = outputDir,
				NamespaceCs = "MyApp.Constants"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			repoMock.Verify(r => r.GetCurrentDefaultSolutionAsync(), Times.Once);

			serviceMock.Verify(s => s.GenerateAsync(
				It.IsAny<IOrganizationServiceAsync2>(),
				It.Is<ConstantsOutputRequest>(r => r.SolutionName == "DefaultSolution"),
				It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_FailsWhenNoSolutionAndNoDefault()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			repoMock
				.Setup(r => r.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync((string?)null);

			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = null,
				OutputCs = Path.GetTempPath(),
				NamespaceCs = "MyApp.Constants"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			serviceMock.Verify(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_UsesSolutionFromCommandWhenProvided()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((3, 3));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = "ExplicitSolution",
				OutputCs = outputDir,
				NamespaceCs = "MyApp.Constants"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			repoMock.Verify(r => r.GetCurrentDefaultSolutionAsync(), Times.Never);

			serviceMock.Verify(s => s.GenerateAsync(
				It.IsAny<IOrganizationServiceAsync2>(),
				It.Is<ConstantsOutputRequest>(r => r.SolutionName == "ExplicitSolution"),
				It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Output folder creation ────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_CreatesOutputFoldersWhenTheyDoNotExist()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((2, 2));

			var tempCs = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			var tempJs = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			try
			{
				var command = new GenerateLateBoundConstantsCommand
				{
					Solution = "MySolution",
					OutputCs = tempCs,
					NamespaceCs = "MyApp.Constants",
					OutputJs = tempJs,
					NamespaceJs = "MyApp"
				};

				var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsTrue(Directory.Exists(tempCs), "C# output folder should have been created");
				Assert.IsTrue(Directory.Exists(tempJs), "JS output folder should have been created");
			}
			finally
			{
				if (Directory.Exists(tempCs)) Directory.Delete(tempCs);
				if (Directory.Exists(tempJs)) Directory.Delete(tempJs);
			}
		}

		// ── Happy-path: service is called with correct request ────────────────

		[TestMethod]
		public async Task ExecuteAsync_CallsGenerateServiceWithCorrectRequest()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			ConstantsOutputRequest? capturedRequest = null;
			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.Callback<IOrganizationServiceAsync2, ConstantsOutputRequest, CancellationToken>((_, r, _) => capturedRequest = r)
				.ReturnsAsync((4, 2));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = "TestSolution",
				OutputCs = outputDir,
				NamespaceCs = "TestApp.Constants",
				OutputJs = outputDir,
				NamespaceJs = "TestApp",
				JsHeader = "// header",
				WithTypes = false,
				WithDescriptions = false
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedRequest);
			Assert.AreEqual("TestSolution", capturedRequest.SolutionName);
			Assert.AreEqual(outputDir, capturedRequest.OutputCs);
			Assert.AreEqual("TestApp.Constants", capturedRequest.NamespaceCs);
			Assert.AreEqual(outputDir, capturedRequest.OutputJs);
			Assert.AreEqual("TestApp", capturedRequest.NamespaceJs);
			Assert.AreEqual("// header", capturedRequest.JsHeader);
			Assert.IsFalse(capturedRequest.WithTypes);
			Assert.IsFalse(capturedRequest.WithDescriptions);
		}

		// ── Result values ──────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ReturnsSuccessWithFileCounts()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((7, 5));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = "MySolution",
				OutputCs = outputDir,
				NamespaceCs = "MyApp.Constants",
				OutputJs = outputDir,
				NamespaceJs = "MyApp"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(7, result["CsFilesGenerated"]);
			Assert.AreEqual(5, result["JsFilesGenerated"]);
			Assert.AreEqual("MySolution", result["Solution"]);
		}

		// ── Exception handling ─────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_FailsWhenInvalidOperationExceptionThrown()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("Solution not found: BadSolution"));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = "BadSolution",
				OutputCs = outputDir,
				NamespaceCs = "MyApp.Constants"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Solution not found");
		}

		[TestMethod]
		public async Task ExecuteAsync_FailsWhenDataverseThrows()
		{
			var (output, repoMock, _, serviceMock) = CreateMocks();

			serviceMock
				.Setup(s => s.GenerateAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<ConstantsOutputRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
					new Microsoft.Xrm.Sdk.OrganizationServiceFault(),
					"Simulated Dataverse fault"));

			var outputDir = Path.GetTempPath();
			var command = new GenerateLateBoundConstantsCommand
			{
				Solution = "MySolution",
				OutputCs = outputDir,
				NamespaceCs = "MyApp.Constants"
			};

			var executor = new GenerateLateBoundConstantsCommandExecutor(output, repoMock.Object, serviceMock.Object);
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
		}
	}
}
