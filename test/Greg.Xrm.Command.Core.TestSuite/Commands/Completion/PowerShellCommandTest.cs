namespace Greg.Xrm.Command.Commands.Completion
{
	[TestClass]
	public class PowerShellCommandTest
	{
		[TestMethod]
		public void ParseShouldWork()
		{
			var command = Utility.TestParseCommand<PowerShellCommand>("completion", "powershell");

			Assert.IsNotNull(command);
		}
	}
}
