namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[TestClass]
	public class GetQueueStatusCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetQueueStatusCommand>("unifiedrouting", "queueStatus", "--queue", "QUEUENAME");
			Assert.AreEqual("QUEUENAME", command.Queue);
			Assert.IsNull(command.DateTimeFilter);
		}
	}
}
