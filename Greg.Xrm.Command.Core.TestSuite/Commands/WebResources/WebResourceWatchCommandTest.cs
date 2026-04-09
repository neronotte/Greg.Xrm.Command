namespace Greg.Xrm.Command.Commands.WebResources
{
	[TestClass]
	public class WebResourceWatchCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredArgumentShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceWatchCommand>(
				"webresource", "watch",
				"--config", "webresources.json");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.AreEqual(500, command.DebounceMs);
			Assert.IsFalse(command.Publish);
			Assert.IsFalse(command.Poll);
			Assert.AreEqual(2000, command.PollIntervalMs);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceWatchCommand>(
				"webresource", "watch",
				"--config", "webresources.json",
				"--solution", "MySolution",
				"--debounce", "1000",
				"--publish",
				"--poll",
				"--poll-interval", "5000");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual(1000, command.DebounceMs);
			Assert.IsTrue(command.Publish);
			Assert.IsTrue(command.Poll);
			Assert.AreEqual(5000, command.PollIntervalMs);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<WebResourceWatchCommand>(
				"webresource", "watch",
				"-c", "webresources.json",
				"-s", "MySolution",
				"-p");

			Assert.AreEqual("webresources.json", command.ConfigPath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.Publish);
		}
	}
}
