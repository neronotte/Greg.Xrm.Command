namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class TranslateCommandTest
	{
		[TestMethod]
		public void ParseCanonicalCommandShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"view", "translate",
				"--name", "Active Accounts",
				"--table", "account",
				"--labels", "1033:\"Active Accounts\"",
				"--description", "1033:\"Active customer accounts\"");

			Assert.AreEqual("Active Accounts", command.Name);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("1033:\"Active Accounts\"", command.Labels);
			Assert.AreEqual("1033:\"Active customer accounts\"", command.Description);
			Assert.IsFalse(command.NoPublish);
		}

		[TestMethod]
		public void ParsePluralAliasAndShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"views", "translate",
				"-n", "Active Accounts",
				"-t", "account",
				"-l", "1033:\"Active Accounts\"",
				"-np");

			Assert.AreEqual("Active Accounts", command.Name);
			Assert.IsTrue(command.NoPublish);
		}
	}
}
