namespace Greg.Xrm.Command.Commands.WebResources
{
	[TestClass]
	public class WebResourceMapCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredArgumentShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceMapCommand>(
				"webresource", "map",
				"--config", "webresources.json");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsFalse(command.DryRun);
			Assert.IsFalse(command.Publish);
			Assert.IsFalse(command.Force);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceMapCommand>(
				"webresource", "map",
				"--config", "webresources.json",
				"--solution", "MySolution",
				"--dry-run",
				"--publish",
				"--force");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.DryRun);
			Assert.IsTrue(command.Publish);
			Assert.IsTrue(command.Force);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceMapCommand>(
				"webresource", "map",
				"-c", "webresources.json",
				"-s", "MySolution",
				"-p");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.Publish);
		}
	}
}
