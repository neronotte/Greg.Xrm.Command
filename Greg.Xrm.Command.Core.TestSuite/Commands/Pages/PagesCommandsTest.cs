namespace Greg.Xrm.Command.Commands.Pages
{
	[TestClass]
	public class PagesCommandsTest
	{
		[TestMethod]
		public void SitePublish_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<PagesSitePublishCommand>(
				"pages", "site", "publish",
				"-s", "C:\\sites\\mysite");

			Assert.AreEqual("C:\\sites\\mysite", command.SourcePath);
			Assert.IsNull(command.WebsiteId);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void SitePublish_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PagesSitePublishCommand>(
				"pages", "site", "publish",
				"-s", "C:\\sites\\mysite",
				"-w", "my-website",
				"--dry-run");

			Assert.AreEqual("C:\\sites\\mysite", command.SourcePath);
			Assert.AreEqual("my-website", command.WebsiteId);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void WebTemplateSync_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<PagesWebTemplateSyncCommand>(
				"pages", "webtemplate", "sync",
				"-s", "env-dev",
				"-t", "env-prod");

			Assert.AreEqual("env-dev", command.SourceEnvironmentId);
			Assert.AreEqual("env-prod", command.TargetEnvironmentId);
			Assert.AreEqual("all", command.SyncType);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void WebTemplateSync_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PagesWebTemplateSyncCommand>(
				"pages", "webtemplate", "sync",
				"-s", "env-dev",
				"-t", "env-prod",
				"--type", "webtemplate",
				"--dry-run");

			Assert.AreEqual("env-dev", command.SourceEnvironmentId);
			Assert.AreEqual("env-prod", command.TargetEnvironmentId);
			Assert.AreEqual("webtemplate", command.SyncType);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void LiquidLint_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<PagesLiquidLintCommand>(
				"pages", "liquid", "lint",
				"-f", "C:\\templates\\header.liquid");

			Assert.AreEqual("C:\\templates\\header.liquid", command.FilePath);
			Assert.IsFalse(command.Strict);
		}

		[TestMethod]
		public void LiquidLint_ParseWithStrictModeShouldWork()
		{
			var command = Utility.TestParseCommand<PagesLiquidLintCommand>(
				"pages", "liquid", "lint",
				"-f", "C:\\templates",
				"--strict");

			Assert.AreEqual("C:\\templates", command.FilePath);
			Assert.IsTrue(command.Strict);
		}
	}
}
