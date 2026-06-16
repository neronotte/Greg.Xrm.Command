namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class DeleteCustomApiCommandTest
	{
		[TestMethod]
		public void ParseUniqueNameLongShouldWork()
		{
			var command = Utility.TestParseCommand<DeleteCustomApiCommand>("customapi", "delete", "--unique-name", "nn_GregSum");
			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseUniqueNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<DeleteCustomApiCommand>("customapi", "delete", "-n", "nn_GregSum");
			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ForceShouldDefaultToFalse()
		{
			var command = Utility.TestParseCommand<DeleteCustomApiCommand>("customapi", "delete", "-n", "nn_GregSum");
			Assert.IsFalse(command.Force);
		}

		[TestMethod]
		public void ForceShouldBeTrueWhenProvided()
		{
			var command = Utility.TestParseCommand<DeleteCustomApiCommand>("customapi", "delete", "-n", "nn_GregSum", "--force");
			Assert.IsTrue(command.Force);
		}
	}
}
