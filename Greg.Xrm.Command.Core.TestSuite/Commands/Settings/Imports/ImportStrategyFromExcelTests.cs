using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	[TestClass]
	public class ImportStrategyFromExcelTests
	{
		[TestMethod]
		public async Task ImportAsync_ShouldParseActionsFromExportWorkbook()
		{
			using var stream = BuildWorkbook(ws =>
			{
				ws.Cell(1, 1).Value = "Settings list";
				ws.Range(2, 5, 2, 6).Merge();
				ws.Cell(2, 5).Value = "App value";

				ws.Cell(3, 1).Value = "Unique Name";
				ws.Cell(3, 2).Value = "Description";
				ws.Cell(3, 3).Value = "Default Value";
				ws.Cell(3, 4).Value = "Env. value";
				ws.Cell(3, 5).Value = "Dataverse Accelerator App";
				ws.Cell(3, 6).Value = "My Custom App";
				ws.Cell(3, 7).Value = "Type";
				ws.Cell(3, 8).Value = "Visible";
				ws.Cell(3, 9).Value = "Overridable on";

				ws.Cell(4, 1).Value = "AllowNotificationsEarlyAccess";
				ws.Cell(4, 3).Value = "false";
				ws.Cell(4, 4).Value = "true";
				ws.Cell(4, 5).Value = "true";
			});

			var strategy = new ImportStrategyFromExcel(stream);

			var actions = await strategy.ImportAsync(CancellationToken.None);

			Assert.AreEqual(3, actions.Count);
			Assert.IsInstanceOfType(actions[0], typeof(ImportActionSetDefaultValue));
			Assert.IsInstanceOfType(actions[1], typeof(ImportActionSetEnvironmentValue));
			Assert.IsInstanceOfType(actions[2], typeof(ImportActionSetAppValue));

			Assert.AreEqual("AllowNotificationsEarlyAccess", GetPrivateString(actions[0], "uniqueName"));
			Assert.AreEqual("false", GetPrivateString(actions[0], "value"));
			Assert.AreEqual("AllowNotificationsEarlyAccess", GetPrivateString(actions[1], "uniqueName"));
			Assert.AreEqual("true", GetPrivateString(actions[1], "value"));
			Assert.AreEqual("AllowNotificationsEarlyAccess", GetPrivateString(actions[2], "uniqueName"));
			Assert.AreEqual("Dataverse Accelerator App", GetPrivateString(actions[2], "appName"));
			Assert.AreEqual("true", GetPrivateString(actions[2], "value"));
		}

		[TestMethod]
		public async Task ImportAsync_ShouldRejectDuplicateSettings()
		{
			using var stream = BuildWorkbook(ws =>
			{
				ws.Cell(1, 1).Value = "Settings list";
				ws.Cell(3, 1).Value = "Unique Name";
				ws.Cell(3, 2).Value = "Description";
				ws.Cell(3, 3).Value = "Default Value";
				ws.Cell(3, 4).Value = "Env. value";
				ws.Cell(3, 5).Value = "App 1";
				ws.Cell(3, 7).Value = "Type";
				ws.Cell(3, 8).Value = "Visible";
				ws.Cell(3, 9).Value = "Overridable on";

				ws.Cell(4, 1).Value = "AllowNotificationsEarlyAccess";
				ws.Cell(5, 1).Value = "allownotificationsearlyaccess";
			});

			var strategy = new ImportStrategyFromExcel(stream);

			var ex = await Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsExceptionAsync<CommandException>(() => strategy.ImportAsync(CancellationToken.None));

			Assert.AreEqual(CommandException.CommandInvalidArgumentValue, ex.ErrorCode);
		}

		private static MemoryStream BuildWorkbook(Action<IXLWorksheet> configure)
		{
			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Settings");
			configure(worksheet);

			var stream = new MemoryStream();
			workbook.SaveAs(stream);
			stream.Position = 0;
			return stream;
		}

		private static string GetPrivateString(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
			return (string)field!.GetValue(target)!;
		}
	}
}
