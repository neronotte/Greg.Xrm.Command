namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class ListCommandTest
	{
		// ── no options — valid because --user is optional ──────────────────────

		[TestMethod]
		public void ParseWithNoOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"usersettings", "list");

			Assert.IsNull(command.UserDomainName);
		}

		// ── --user / -u ───────────────────────────────────────────────────────

		[TestMethod]
		public void UserOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"usersettings", "list",
				"--user", @"DOMAIN\john.doe");

			Assert.AreEqual(@"DOMAIN\john.doe", command.UserDomainName);
		}

		[TestMethod]
		public void UserOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"usersettings", "list",
				"-u", "john.doe@contoso.com");

			Assert.AreEqual("john.doe@contoso.com", command.UserDomainName);
		}

		[TestMethod]
		public void UserShouldBeNullWhenOmitted()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"usersettings", "list");

			Assert.IsNull(command.UserDomainName);
		}
	}
}
