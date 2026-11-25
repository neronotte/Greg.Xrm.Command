namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class SetFetchXmlCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesAllOptions(bool shortNames)
		{
			var command = Utility.TestParseCommand<SetFetchXmlCommand>("view", "setFetchXml",
				shortNames ? "-n" : "--name", "My View",
				shortNames ? "-t" : "--table", "account",
				shortNames ? "-f" : "--fetchXml", "<fetch/>",
				shortNames ? "-q" : "--type", "UserQuery",
				shortNames ? "-p" : "--publish", "true");
			Assert.AreEqual("My View", command.ViewName);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("<fetch/>", command.FetchXml);
			Assert.AreEqual(QueryType1.UserQuery, command.QueryType);
			Assert.IsTrue(command.Publish);
		}

		[TestMethod]
		public void DefaultsToUnpublishedSystemView()
		{
			var command = Utility.TestParseCommand<SetFetchXmlCommand>("view", "setFetchXml", "--name", "My View", "--fetchXml", "<fetch/>");
			Assert.AreEqual(QueryType1.SavedQuery, command.QueryType);
			Assert.IsFalse(command.Publish);
			Assert.IsNull(command.TableName);
		}
	}
}
