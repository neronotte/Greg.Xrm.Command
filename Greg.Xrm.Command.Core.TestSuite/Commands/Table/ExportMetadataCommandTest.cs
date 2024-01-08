using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;
using OfficeOpenXml;
using System.Diagnostics;

namespace Greg.Xrm.Command.Commands.Table
{
    [TestClass]
	public class ExportMetadataCommandTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_ExecuteExportExcel()
		{
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

			var storage = new Storage();
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository(storage);
			var repository = new OrganizationServiceRepository(settingsRepository);

			var exportMetadataStrategyFactory = new ExportMetadataStrategyFactory(output);

			var command = new ExportMetadataCommand
			{
				Format = ExportMetadataFormat.Excel,
				OutputFilePath = @"C:\temp\",
				TableSchemaName = "ava_practice",
				AutoOpenFile = true,
			};


			var executor = new ExportMetadataCommandExecutor(output, repository, exportMetadataStrategyFactory);

			executor.ExecuteAsync(command, CancellationToken.None).Wait();


			var outputText = output.ToString();

			Debug.WriteLine(outputText);

			Assert.IsNotNull(outputText);
		}
	}
}
