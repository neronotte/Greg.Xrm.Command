namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class SetFilterCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesAllOptions(bool shortNames)
		{
			var command = Utility.TestParseCommand<SetFilterCommand>("view", "setFilter",
				shortNames ? "-n" : "--name", "My View",
				shortNames ? "-t" : "--table", "account",
				shortNames ? "-f" : "--filter", "<filter/>",
				shortNames ? "-q" : "--type", "UserQuery",
				shortNames ? "-p" : "--publish", "true");
			Assert.AreEqual("My View", command.ViewName);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("<filter/>", command.Filter);
			Assert.AreEqual(QueryType1.UserQuery, command.QueryType);
			Assert.IsTrue(command.Publish);
		}

		[TestMethod]
		public void DefaultsToUnpublishedSystemView()
		{
			var command = Utility.TestParseCommand<SetFilterCommand>("view", "setFilter", "--name", "My View", "--filter", "<filter/>");
			Assert.AreEqual(QueryType1.SavedQuery, command.QueryType);
			Assert.IsFalse(command.Publish);
			Assert.IsNull(command.TableName);
		}
	}
}
