namespace Greg.Xrm.Command.Commands.ConnectionRef
{
	[TestClass]
	public class ConnectionRefMapCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectionRefMapCommand>(
				"connection-ref", "map");

			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsNull(command.ConnectorId);
			Assert.AreEqual("table", command.Format);
			Assert.IsFalse(command.Interactive);
		}

		[TestMethod]
		public void ParseWithFiltersShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectionRefMapCommand>(
				"connection-ref", "map",
				"-s", "MySolution",
				"-c", "shared_commondataservice",
				"-f", "json",
				"-i");

			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("shared_commondataservice", command.ConnectorId);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.Interactive);
		}
	}
}
