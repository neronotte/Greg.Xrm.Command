namespace Greg.Xrm.Command.Commands.Completion
{
	[TestClass]
	public class ExportCommandTest
	{
		[TestMethod]
		public void ParseShouldWork()
		{
			var command = Utility.TestParseCommand<ExportCommand>("completion", "export");

			Assert.IsNotNull(command);
		}
	}
}
