namespace Greg.Xrm.Command.Commands.WebResources
{
	[TestClass]
	public class WebResourceMapCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredConfigShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WebResourceMapCommand>(
				"webresource", "map",
				"-c", "mapping.json");

			Assert.AreEqual("mapping.json", command.ConfigPath);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsFalse(command.DryRun);
			Assert.IsFalse(command.Publish);
			Assert.IsFalse(command.Force);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WebResourceMapCommand>(
				"webresource", "map",
				"-c", "mapping.json",
				"-s", "MySolution",
				"--dry-run",
				"-p",
				"-f");

			Assert.AreEqual("mapping.json", command.ConfigPath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.DryRun);
			Assert.IsTrue(command.Publish);
			Assert.IsTrue(command.Force);
		}
	}

	[TestClass]
	public class WebResourceWatchCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredConfigShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WebResourceWatchCommand>(
				"webresource", "watch",
				"-c", "mapping.json");

			Assert.AreEqual("mapping.json", command.ConfigPath);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.AreEqual(500, command.DebounceMs);
			Assert.IsFalse(command.Publish);
			Assert.IsFalse(command.Poll);
			Assert.AreEqual(2000, command.PollIntervalMs);
		}

		[TestMethod]
		public void ParseWithPollingOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WebResourceWatchCommand>(
				"webresource", "watch",
				"-c", "mapping.json",
				"--poll",
				"--poll-interval", "5000",
				"--debounce", "1000",
				"-p");

			Assert.IsTrue(command.Poll);
			Assert.AreEqual(5000, command.PollIntervalMs);
			Assert.AreEqual(1000, command.DebounceMs);
			Assert.IsTrue(command.Publish);
		}
	}
}
