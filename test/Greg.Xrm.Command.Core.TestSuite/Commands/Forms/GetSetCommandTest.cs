using Greg.Xrm.Command.Services.Forms;
using System.IO.Compression;
using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class GetSetCommandTest : CommandExecutorTestBase
	{
		private readonly Mock<IFormRepository> forms = new();
		private readonly Mock<ISolutionRepository> solutions = new();

		[TestMethod]
		public void CommandsParse()
		{
			var get = Utility.TestParseCommand<GetCommand>("forms", "get", "-t", "account", "-o", "form.xml");
			var set = Utility.TestParseCommand<SetCommand>("forms", "set", "-t", "account", "-i", "form.xml", "--fast", "true");
			Assert.AreEqual("form.xml", get.OutputFile);
			Assert.AreEqual("form.xml", set.FileName);
			Assert.IsTrue(set.Fast);
		}

		[TestMethod]
		public async Task GetPrintsProgressAndRawXml()
		{
			SetupForm("<form><tabs /></form>");
			var executor = new GetCommandExecutor(OrganizationServiceRepositoryMock.Object, forms.Object, Output);

			var result = await executor.ExecuteAsync(new GetCommand { TableName = "account" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual("Connecting to the current dataverse environment...<Green>Done</Green>" + Environment.NewLine +
				"<form><tabs /></form>" + Environment.NewLine, Output.ToString());
		}

		[TestMethod]
		public async Task GetWritesFile()
		{
			SetupForm("<form><tabs /></form>");
			var path = Path.GetTempFileName();
			try
			{
				var executor = new GetCommandExecutor(OrganizationServiceRepositoryMock.Object, forms.Object, Output);
				var result = await executor.ExecuteAsync(new GetCommand { TableName = "account", OutputFile = path }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess);
				Assert.AreEqual("<form><tabs /></form>", await File.ReadAllTextAsync(path));
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		public async Task SetRejectsInvalidRootBeforeConnecting()
		{
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<ImportExportXml />");
				var executor = NewSetExecutor();
				var result = await executor.ExecuteAsync(new SetCommand { TableName = "account", FileName = path }, CancellationToken.None);
				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "<form>");
				OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentConnectionAsync(), Times.Never);
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		public async Task SetFastUpdatesFormAndPublishesTable()
		{
			var form = SetupForm("<form><tabs /></form>");
			var path = Path.GetTempFileName();
			var backup = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><columns><column width=\"100%\" /></columns></tab></tabs></form>");
				var executor = NewSetExecutor();
				var result = await executor.ExecuteAsync(new SetCommand { TableName = "account", FileName = path, BackupFile = backup, Fast = true, Publish = true }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess);
				Assert.AreEqual("<form><tabs /></form>", await File.ReadAllTextAsync(backup));
				OrganizationServiceMock.Verify(crm => crm.UpdateAsync(It.Is<Entity>(entity => entity.Id == form.Id &&
					entity.GetAttributeValue<string>("formxml")!.Contains("name=\"new\"")), It.IsAny<CancellationToken>()), Times.Once);
				OrganizationServiceMock.Verify(crm => crm.ExecuteAsync(It.Is<PublishXmlRequest>(request => request.ParameterXml.Contains("account")), It.IsAny<CancellationToken>()), Times.Once);
			}
			finally { File.Delete(path); File.Delete(backup); }
		}

		[TestMethod]
		public async Task SetUsesTemporarySolutionByDefault()
		{
			var form = SetupForm("<form><tabs /></form>");
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><columns><column width=\"100%\" /></columns></tab></tabs></form>");
				var temporary = new Mock<ITemporarySolution>();
				temporary.Setup(temp => temp.DownloadAsync()).ReturnsAsync(new SolutionZipArchive(CreateSolutionZip()));
				solutions.Setup(repo => repo.CreateHoldingSolutionAsync(OrganizationServiceMock.Object, null)).ReturnsAsync((CommandResult.Success(), temporary.Object));
				byte[]? uploaded = null;
				temporary.Setup(temp => temp.UploadAndPublishAsync(It.IsAny<byte[]>(), It.IsAny<string[]>()))
					.Callback<byte[], string[]>((bytes, _) => uploaded = bytes).Returns(Task.CompletedTask);

				var result = await NewSetExecutor().ExecuteAsync(new SetCommand { TableName = "account", FileName = path, Publish = true }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				temporary.Verify(temp => temp.AddComponentAsync(form.Id, ComponentType.SystemForm), Times.Once);
				Assert.IsNotNull(uploaded);
				using var stream = new MemoryStream(uploaded);
				using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
				using var reader = new StreamReader(archive.GetEntry("customizations.xml")!.Open());
				StringAssert.Contains(await reader.ReadToEndAsync(), "name=\"new\"");
			}
			finally { File.Delete(path); }
		}

		private Form SetupForm(string xml)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = "Information";
			entity["formxml"] = xml;
			var form = new Form(entity);
			forms.Setup(repo => repo.GetByNameAsync(OrganizationServiceMock.Object, "account", It.IsAny<string>(), It.IsAny<bool>()))
				.ReturnsAsync((CommandResult.Success(), form));
			return form;
		}

		private SetCommandExecutor NewSetExecutor() => new(OrganizationServiceRepositoryMock.Object, forms.Object, solutions.Object, new FormXmlValidator(), Output);


		private static byte[] CreateSolutionZip()
		{
			using var stream = new MemoryStream();
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
			using (var writer = new StreamWriter(archive.CreateEntry("customizations.xml").Open()))
				writer.Write(new XDocument(new XElement("ImportExportXml", new XElement("Entities", new XElement("Entity",
					new XElement("FormXml", new XElement("forms", new XElement("systemform", new XElement("form", new XElement("tabs"))))))))).ToString());
			return stream.ToArray();
		}
	}
}
