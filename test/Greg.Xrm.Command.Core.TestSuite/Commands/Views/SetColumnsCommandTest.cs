namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class SetColumnsCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesAllOptions(bool shortNames)
		{
			var command = Utility.TestParseCommand<SetColumnsCommand>("view", "setColumns",
				shortNames ? "-n" : "--name", "My View",
				shortNames ? "-t" : "--table", "account",
				shortNames ? "-c" : "--columns", "name,telephone1",
				shortNames ? "-q" : "--type", "UserQuery",
				shortNames ? "-p" : "--publish", "true");
			Assert.AreEqual("My View", command.ViewName);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("name,telephone1", command.Columns);
			Assert.AreEqual(QueryType1.UserQuery, command.QueryType);
			Assert.IsTrue(command.Publish);
		}

		[TestMethod]
		public void DefaultsToUnpublishedSystemView()
		{
			var command = Utility.TestParseCommand<SetColumnsCommand>("view", "setColumns", "--name", "My View", "--columns", "name,telephone1");
			Assert.AreEqual(QueryType1.SavedQuery, command.QueryType);
			Assert.IsFalse(command.Publish);
			Assert.IsNull(command.TableName);
		}
	}
}
