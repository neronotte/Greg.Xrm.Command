namespace Greg.Xrm.Command.Commands.Storage
{
	[TestClass]
	public class StorageAnalyticsCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<StorageAnalyticsCommand>(
				"storage", "analytics");

			Assert.AreEqual(20, command.TopN);
			Assert.AreEqual("table", command.Format);
			Assert.IsFalse(command.IncludeRecommendations);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<StorageAnalyticsCommand>(
				"storage", "analytics",
				"--top", "50",
				"-f", "json",
				"-r");

			Assert.AreEqual(50, command.TopN);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.IncludeRecommendations);
		}
	}
}
