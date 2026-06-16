namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class ListCustomApiCommandTest
	{
		[TestMethod]
		public void ParseFilterLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "--filter", "Greg");
			Assert.AreEqual("Greg", command.Filter);
		}

		[TestMethod]
		public void ParseFilterShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "-f", "Greg");
			Assert.AreEqual("Greg", command.Filter);
		}

		[TestMethod]
		public void ParsePublisherLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "--publisher", "nn");
			Assert.AreEqual("nn", command.Publisher);
		}

		[TestMethod]
		public void ParsePublisherShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "-pub", "nn");
			Assert.AreEqual("nn", command.Publisher);
		}

		[TestMethod]
		public void ParseTypeActionShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "-t", "Action");
			Assert.AreEqual(CustomApiType.Action, command.Type);
		}

		[TestMethod]
		public void ParseTypeFunctionShouldWork()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list", "-t", "Function");
			Assert.AreEqual(CustomApiType.Function, command.Type);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<ListCustomApiCommand>("customapi", "list");
			Assert.IsNull(command.Filter);
			Assert.IsNull(command.Publisher);
			Assert.IsNull(command.Type);
		}
	}
}
