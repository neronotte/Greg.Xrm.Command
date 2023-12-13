namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[TestClass]
	public class GetAgentStatusCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetAgentStatusCommand>("unifiedrouting", "agentStatus", "--agentPrimaryEmail", "francesco.catino@avanade.com");
		}
	}
}
