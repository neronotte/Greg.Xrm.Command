using Greg.Xrm.Command.Services.Forms;
using System.IO.Compression;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class SetCommandTest : CommandExecutorTestBase
	{
		private readonly Mock<IFormRepository> forms = new();
		private readonly Mock<ISolutionRepository> solutions = new();

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task BackupPathComparisonUsesPlatformCaseSensitivity(bool changeCase)
		{
			SetupForm();
			var folder = Utility.CreateTempFolder();
			var input = Path.Combine(folder, "form.xml");
			var backup = Path.Combine(folder, changeCase ? "FORM.xml" : "form.xml");
			try
			{
				await File.WriteAllTextAsync(input, FormXmlValidatorTest.ValidFormXml);
				var result = await NewExecutor().ExecuteAsync(new SetCommand
				{
					TableName = "account", FileName = input, BackupFile = backup, Fast = true
				}, CancellationToken.None);

				if (changeCase && !OperatingSystem.IsWindows())
				{
					Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
					Assert.AreEqual(FormXmlValidatorTest.ValidFormXml, await File.ReadAllTextAsync(input));
					Assert.AreEqual("<form><tabs /></form>", await File.ReadAllTextAsync(backup));
				}
				else
				{
					Assert.IsFalse(result.IsSuccess);
					StringAssert.Contains(result.ErrorMessage, "must differ");
					OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentConnectionAsync(), Times.Never);
				}
			}
			finally { Directory.Delete(folder, true); }
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task RejectsSchemaInvalidXmlBeforeConnectingOrWritingBackup(bool fast)
		{
			var path = Path.GetTempFileName();
			var backup = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\" /></tabs></form>");
				await File.WriteAllTextAsync(backup, "existing backup");
				var result = await NewExecutor().ExecuteAsync(new SetCommand
				{
					TableName = "account", FileName = path, BackupFile = backup, Fast = fast, Publish = true
				}, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "Form XML validation failed");
				StringAssert.Contains(result.ErrorMessage, "columns");
				Assert.AreEqual("existing backup", await File.ReadAllTextAsync(backup));
				OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentConnectionAsync(), Times.Never);
				OrganizationServiceMock.Verify(crm => crm.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
				solutions.Verify(repo => repo.CreateHoldingSolutionAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<string?>()), Times.Never);
			}
			finally { File.Delete(path); File.Delete(backup); }
		}

		[TestMethod]
		public async Task AcceptsCompleteFormAndSkipsUnchangedXml()
		{
			var form = SetupForm();
			form.formxml = FormXmlValidatorTest.ValidFormXml;
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<?xml version=\"1.0\"?>" + form.formxml);
				var result = await NewExecutor().ExecuteAsync(new SetCommand { TableName = "account", FileName = path, Fast = true }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				StringAssert.Contains(Output.ToString(), "unchanged");
				OrganizationServiceMock.Verify(crm => crm.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		public void ParsesFileAndFormOptions()
		{
			var command = Utility.TestParseCommand<SetCommand>("forms", "set", "-t", "account", "-f", "Information", "-i", "form.xml");
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("Information", command.FormName);
			Assert.AreEqual("form.xml", command.FileName);
			Assert.IsFalse(command.Fast);
			Assert.IsFalse(command.Publish);
			var publishing = Utility.TestParseCommand<SetCommand>("forms", "set", "-t", "account", "-i", "form.xml", "--publish", "true");
			Assert.IsTrue(publishing.Publish);
		}

		[TestMethod]
		public async Task RejectsInvalidXmlBeforeConnecting()
		{
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<ImportExportXml />");
				var result = await NewExecutor().ExecuteAsync(new SetCommand { TableName = "account", FileName = path }, CancellationToken.None);
				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "<form>");
				OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentConnectionAsync(), Times.Never);
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task RejectsSolutionDiffBeforeConnecting(bool fast)
		{
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><cell id=\"{1}\" solutionaction=\"Removed\" /></tab></tabs></form>");
				var result = await NewExecutor().ExecuteAsync(new SetCommand { TableName = "account", FileName = path, Fast = fast }, CancellationToken.None);
				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "solutionaction");
				OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentConnectionAsync(), Times.Never);
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		public async Task DirectUpdateWarnsAndDoesNotPublishByDefault()
		{
			var form = SetupForm();
			var path = Path.GetTempFileName();
			var backup = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><columns><column width=\"100%\" /></columns></tab></tabs></form>");
				var result = await NewExecutor().ExecuteAsync(new SetCommand
				{
					TableName = "account", FileName = path, BackupFile = backup, Fast = true
				}, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.AreEqual("<form><tabs /></form>", await File.ReadAllTextAsync(backup));
				OrganizationServiceMock.Verify(crm => crm.UpdateAsync(It.Is<Entity>(entity => entity.Id == form.Id &&
					entity.GetAttributeValue<string>("formxml")!.Contains("name=\"new\"")), It.IsAny<CancellationToken>()), Times.Once);
				OrganizationServiceMock.Verify(crm => crm.ExecuteAsync(It.IsAny<PublishXmlRequest>(), It.IsAny<CancellationToken>()), Times.Never);
				StringAssert.Contains(Output.ToString(), "formjson may remain out of sync");
			}
			finally { File.Delete(path); File.Delete(backup); }
		}

		[TestMethod]
		public async Task DirectUpdatePublishesOnlyWhenRequested()
		{
			SetupForm();
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><columns><column width=\"100%\" /></columns></tab></tabs></form>");
				var result = await NewExecutor().ExecuteAsync(new SetCommand
				{
					TableName = "account", FileName = path, Fast = true, Publish = true
				}, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				OrganizationServiceMock.Verify(crm => crm.ExecuteAsync(It.Is<PublishXmlRequest>(request => request.ParameterXml.Contains("account")), It.IsAny<CancellationToken>()), Times.Once);
				StringAssert.Contains(Output.ToString(), "formjson may remain out of sync");
			}
			finally { File.Delete(path); }
		}

		[TestMethod]
		public async Task TemporarySolutionUploadDoesNotPublish()
		{
			var entity = new Entity("solution", Guid.NewGuid());
			entity["uniquename"] = "temporary_form_update";
			var temporary = new TemporarySolution(OrganizationServiceMock.Object, Output, new TestSolution(entity));

			await temporary.UploadAsync([1, 2, 3]);

			OrganizationServiceMock.Verify(crm => crm.ExecuteAsync(It.IsAny<ImportSolutionRequest>()), Times.Once);
			OrganizationServiceMock.Verify(crm => crm.ExecuteAsync(It.IsAny<PublishXmlRequest>()), Times.Never);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task SolutionUpdatePublishesOnlyWhenRequested(bool publish)
		{
			var form = SetupForm();
			var path = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(path, "<form><tabs><tab name=\"new\"><columns><column width=\"100%\" /></columns></tab></tabs></form>");
				var temporary = new Mock<ITemporarySolution>();
				temporary.Setup(temp => temp.DownloadAsync()).ReturnsAsync(new SolutionZipArchive(CreateSolutionZip()));
				solutions.Setup(repo => repo.CreateHoldingSolutionAsync(OrganizationServiceMock.Object, null)).ReturnsAsync((CommandResult.Success(), temporary.Object));
				byte[]? uploaded = null;
				temporary.Setup(temp => temp.UploadAsync(It.IsAny<byte[]>()))
					.Callback<byte[]>(bytes => uploaded = bytes).Returns(Task.CompletedTask);
				temporary.Setup(temp => temp.UploadAndPublishAsync(It.IsAny<byte[]>(), It.IsAny<string[]>()))
					.Callback<byte[], string[]>((bytes, _) => uploaded = bytes).Returns(Task.CompletedTask);

				var result = await NewExecutor().ExecuteAsync(new SetCommand { TableName = "account", FileName = path, Publish = publish }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				temporary.Verify(temp => temp.AddComponentAsync(form.Id, ComponentType.SystemForm), Times.Once);
				temporary.Verify(temp => temp.UploadAsync(It.IsAny<byte[]>()), publish ? Times.Never : Times.Once);
				temporary.Verify(temp => temp.UploadAndPublishAsync(It.IsAny<byte[]>(), It.IsAny<string[]>()), publish ? Times.Once : Times.Never);
				Assert.IsNotNull(uploaded);
				using var stream = new MemoryStream(uploaded);
				using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
				using var reader = new StreamReader(archive.GetEntry("customizations.xml")!.Open());
				StringAssert.Contains(await reader.ReadToEndAsync(), "name=\"new\"");
			}
			finally { File.Delete(path); }
		}

		private SetCommandExecutor NewExecutor() => new(OrganizationServiceRepositoryMock.Object, forms.Object, solutions.Object, new FormXmlValidator(), Output);

		private Form SetupForm()
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = "Information";
			entity["formxml"] = "<form><tabs /></form>";
			var form = new Form(entity);
			forms.Setup(repo => repo.GetByNameAsync(OrganizationServiceMock.Object, "account", It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync((CommandResult.Success(), form));
			return form;
		}


		private sealed class TestSolution(Entity entity) : Greg.Xrm.Command.Model.Solution(entity);

		private static byte[] CreateSolutionZip()
		{
			using var stream = new MemoryStream();
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
			using (var writer = new StreamWriter(archive.CreateEntry("customizations.xml").Open()))
				writer.Write("<ImportExportXml><Entities><Entity><FormXml><forms><systemform><form><tabs /></form></systemform></forms></FormXml></Entity></Entities></ImportExportXml>");
			return stream.ToArray();
		}
	}
}
