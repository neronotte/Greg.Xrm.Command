namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class CreateCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesAllOptions(bool shortNames)
		{
			var command = Utility.TestParseCommand<CreateCommand>("view", "create",
				shortNames ? "-n" : "--name", "My View",
				shortNames ? "-f" : "--fetchXml", "<fetch/>",
				shortNames ? "-q" : "--type", "UserQuery",
				shortNames ? "-p" : "--publish", "true");
			Assert.AreEqual("My View", command.ViewName);
			Assert.AreEqual("<fetch/>", command.FetchXml);
			Assert.AreEqual(QueryType1.UserQuery, command.QueryType);
			Assert.IsTrue(command.Publish);
		}

		[TestMethod]
		public void DefaultsToUnpublishedSystemView()
		{
			var command = Utility.TestParseCommand<CreateCommand>("view", "create", "--name", "My View", "--fetchXml", "<fetch/>");
			Assert.AreEqual(QueryType1.SavedQuery, command.QueryType);
			Assert.IsFalse(command.Publish);
		}
	}
}
