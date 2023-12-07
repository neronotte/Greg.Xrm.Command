namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[TestClass]
	public class GetQueueStatusCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetAgentStatusCommand>("unifiedrouting", "queuestatus", "--queue", "QUEUENAME");
		}
	}
}
