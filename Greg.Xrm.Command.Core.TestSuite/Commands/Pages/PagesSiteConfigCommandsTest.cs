namespace Greg.Xrm.Command.Commands.Pages
{
	[TestClass]
	public class PagesSiteConfigCommandsTest
	{
		[TestMethod]
		public void PagesSiteConfigExportParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PagesSiteConfigExportCommand>(
				"pages", "site", "config", "export",
				"-s", "my-site",
				"-o", "./output");

			Assert.AreEqual("my-site", command.SiteId);
			Assert.AreEqual("./output", command.OutputPath);
			Assert.AreEqual("all", command.Scope);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void PagesSiteConfigExportParseWithScopeShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PagesSiteConfigExportCommand>(
				"pages", "site", "config", "export",
				"-s", "site-123",
				"-o", "./config-export",
				"--scope", "auth",
				"-f", "xml");

			Assert.AreEqual("site-123", command.SiteId);
			Assert.AreEqual("./config-export", command.OutputPath);
			Assert.AreEqual("auth", command.Scope);
			Assert.AreEqual("xml", command.Format);
		}

		[TestMethod]
		public void PagesSiteConfigImportParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PagesSiteConfigImportCommand>(
				"pages", "site", "config", "import",
				"-s", "my-site",
				"-i", "./config-export");

			Assert.AreEqual("my-site", command.SiteId);
			Assert.AreEqual("./config-export", command.InputPath);
			Assert.AreEqual("all", command.Scope);
			Assert.IsFalse(command.Force);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void PagesSiteConfigImportParseWithForceAndDryRunShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PagesSiteConfigImportCommand>(
				"pages", "site", "config", "import",
				"-s", "site-456",
				"-i", "./config",
				"--scope", "navigation",
				"-f",
				"--dry-run");

			Assert.AreEqual("site-456", command.SiteId);
			Assert.AreEqual("./config", command.InputPath);
			Assert.AreEqual("navigation", command.Scope);
			Assert.IsTrue(command.Force);
			Assert.IsTrue(command.DryRun);
		}
	}
}

namespace Greg.Xrm.Command.Commands.Governance
{
	[TestClass]
	public class ApiRateLimitMonitorCommandTest
	{
		[TestMethod]
		public void ApiRateLimitMonitorParseWithDefaultsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<ApiRateLimitMonitorCommand>(
				"api", "ratelimit", "monitor");

			Assert.AreEqual("hour", command.Period);
			Assert.AreEqual(80, command.Threshold);
			Assert.AreEqual("table", command.Format);
			Assert.IsFalse(command.Alert);
		}

		[TestMethod]
		public void ApiRateLimitMonitorParseWithCustomOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<ApiRateLimitMonitorCommand>(
				"api", "ratelimit", "monitor",
				"-p", "day",
				"-t", "90",
				"-f", "json",
				"--alert");

			Assert.AreEqual("day", command.Period);
			Assert.AreEqual(90, command.Threshold);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.Alert);
		}
	}
}

namespace Greg.Xrm.Command.Commands.Data
{
	[TestClass]
	public class DataExportImportCommandsTest
	{
		[TestMethod]
		public void DataExportParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<DataExportCommand>(
				"data", "export",
				"-t", "account", "contact",
				"-o", "./export");

			CollectionAssert.AreEqual(new[] { "account", "contact" }, command.Tables);
			Assert.AreEqual("./export", command.OutputPath);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual(500, command.BatchSize);
			Assert.IsFalse(command.IncludeRelationships);
		}

		[TestMethod]
		public void DataExportParseWithSolutionShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<DataExportCommand>(
				"data", "export",
				"-s", "MySolution",
				"-o", "./solution-export",
				"-f", "csv",
				"--include-relationships");

			Assert.IsNull(command.Tables);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("./solution-export", command.OutputPath);
			Assert.AreEqual("csv", command.Format);
			Assert.IsTrue(command.IncludeRelationships);
		}

		[TestMethod]
		public void DataImportParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<DataImportCommand>(
				"data", "import",
				"-i", "./import-data.json",
				"-t", "account");

			Assert.AreEqual("./import-data.json", command.InputPath);
			Assert.AreEqual("account", command.TargetTable);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("upsert", command.Mode);
			Assert.AreEqual(500, command.BatchSize);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void DataImportParseWithDryRunShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<DataImportCommand>(
				"data", "import",
				"-i", "./import-dir",
				"--mode", "create-only",
				"--dry-run");

			Assert.AreEqual("./import-dir", command.InputPath);
			Assert.IsNull(command.TargetTable);
			Assert.AreEqual("create-only", command.Mode);
			Assert.IsTrue(command.DryRun);
		}
	}
}
