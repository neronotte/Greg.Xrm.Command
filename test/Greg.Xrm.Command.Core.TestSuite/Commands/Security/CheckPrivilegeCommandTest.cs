namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class CheckPrivilegeCommandTest
	{
		[TestMethod]
		public void ParseWithoutRecordIdShouldWork()
		{
			var userId = Guid.NewGuid().ToString();
			var command = Utility.TestParseCommand<CheckPrivilegeCommand>("security", "check-privilege", "--user", userId, "--table", "account");

			Assert.AreEqual(userId, command.User);
			Assert.AreEqual("account", command.TableName);
			Assert.IsNull(command.RecordId);
		}

		[TestMethod]
		public void ParseWithoutUserShouldWork()
		{
			var command = Utility.TestParseCommand<CheckPrivilegeCommand>("security", "check-privilege", "--table", "account");

			Assert.IsNull(command.User);
			Assert.AreEqual("account", command.TableName);
		}

		[TestMethod]
		public void ParseWithRecordIdShouldWork()
		{
			var recordId = Guid.NewGuid().ToString();
			var command = Utility.TestParseCommand<CheckPrivilegeCommand>("security", "check-privilege", "-u", "contoso\\john", "-t", "account", "-id", recordId);

			Assert.AreEqual("contoso\\john", command.User);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual(recordId, command.RecordId);
		}
	}
}
