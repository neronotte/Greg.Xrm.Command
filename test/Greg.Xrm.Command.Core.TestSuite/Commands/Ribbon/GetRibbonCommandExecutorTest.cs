using System.IO.Packaging;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class GetRibbonCommandExecutorTest : CommandExecutorTestBase
	{
		private const string RibbonXml = "<RibbonDefinitions><RibbonDefinition><UI /></RibbonDefinition></RibbonDefinitions>";

		private readonly GetRibbonCommandExecutor executor;
		private OrganizationRequest? capturedRequest;

		public GetRibbonCommandExecutorTest()
		{
			this.executor = new GetRibbonCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object);
		}

		// ── helpers ───────────────────────────────────────────────────────────

		/// <summary>
		/// Builds the same payload Dataverse returns: a zip package containing
		/// a single /RibbonXml.xml part (see GetRibbonCommandExecutor.UnzipRibbonXml).
		/// </summary>
		private static byte[] CreateRibbonZip(string xml)
		{
			using var stream = new MemoryStream();
			using (var package = Package.Open(stream, FileMode.Create))
			{
				var part = package.CreatePart(new Uri("/RibbonXml.xml", UriKind.Relative), "text/xml");
				using var partStream = part.GetStream();
				var bytes = Encoding.UTF8.GetBytes(xml);
				partStream.Write(bytes, 0, bytes.Length);
			}
			return stream.ToArray();
		}

		private void SetupEntityRibbonResponse(byte[] payload)
		{
			var response = new RetrieveEntityRibbonResponse();
			response.Results["CompressedEntityXml"] = payload;

			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(r => this.capturedRequest = r)
				.ReturnsAsync(response);
		}

		private void SetupApplicationRibbonResponse(byte[] payload)
		{
			var response = new RetrieveApplicationRibbonResponse();
			response.Results["CompressedApplicationRibbonXml"] = payload;

			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(r => this.capturedRequest = r)
				.ReturnsAsync(response);
		}

		// ── entity ribbon ─────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldRequestEntityRibbon_WhenTableIsSpecified()
		{
			SetupEntityRibbonResponse(CreateRibbonZip(RibbonXml));
			var command = new GetRibbonCommand { EntityName = "account" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var request = this.capturedRequest as RetrieveEntityRibbonRequest;
			Assert.IsNotNull(request, "Executor should send a RetrieveEntityRibbonRequest when --table is set");
			Assert.AreEqual("account", request.EntityName);
			Assert.AreEqual(RibbonLocationFilters.All, request.RibbonLocationFilter);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWriteRibbonXmlToOutput()
		{
			SetupEntityRibbonResponse(CreateRibbonZip(RibbonXml));
			var command = new GetRibbonCommand { EntityName = "account" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), RibbonXml);
		}

		// ── application ribbon ────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldRequestApplicationRibbon_WhenTableIsOmitted()
		{
			SetupApplicationRibbonResponse(CreateRibbonZip(RibbonXml));
			var command = new GetRibbonCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsInstanceOfType(this.capturedRequest, typeof(RetrieveApplicationRibbonRequest),
				"Executor should fall back to the application ribbon when no --table is given");
		}

		// ── file output ───────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSaveRibbonToFile_WhenOutputIsSpecified()
		{
			// AutoRun deliberately stays false: a unit test must not spawn a process.
			SetupEntityRibbonResponse(CreateRibbonZip(RibbonXml));
			var folder = Utility.CreateTempFolder();
			try
			{
				var fileName = Path.Combine(folder, "ribbon.xml");
				var command = new GetRibbonCommand { EntityName = "account", FileName = fileName };

				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsTrue(File.Exists(fileName), "Ribbon file should have been created");
				Assert.AreEqual(RibbonXml, File.ReadAllText(fileName));
			}
			finally
			{
				Utility.DeleteFolder(folder);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenOutputFolderDoesNotExist()
		{
			SetupEntityRibbonResponse(CreateRibbonZip(RibbonXml));
			var fileName = Path.Combine(Path.GetTempPath(), "PACX-does-not-exist-" + Guid.NewGuid(), "ribbon.xml");
			var command = new GetRibbonCommand { EntityName = "account", FileName = fileName };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}

		// ── error handling ────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseCallThrows()
		{
			this.OrganizationServiceMock
				.Setup(s => s.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ThrowsAsync(new InvalidOperationException("Entity with name 'foo' does not exist"));

			var command = new GetRibbonCommand { EntityName = "foo" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exist");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenPayloadIsNotAValidZip()
		{
			SetupEntityRibbonResponse([1, 2, 3, 4]);
			var command = new GetRibbonCommand { EntityName = "account" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
