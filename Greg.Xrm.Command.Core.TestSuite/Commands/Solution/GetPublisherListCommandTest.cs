namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GetPublisherListCommandTest
    {
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetPublisherListCommand>("solution", "getPublisherList", "--verbose");


			Assert.IsTrue(command.Verbose);
		}
	}
}
