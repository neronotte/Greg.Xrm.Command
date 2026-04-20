namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class SetCommandTest
	{
		// ── --key / -k ────────────────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--key", "uilanguageid",
				"--value", "1040");

			Assert.AreEqual("uilanguageid", command.Key);
			Assert.AreEqual("1040", command.Value);
			Assert.IsNull(command.UserDomainName);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"-k", "timeformatcode",
				"-v", "1");

			Assert.AreEqual("timeformatcode", command.Key);
			Assert.AreEqual("1", command.Value);
			Assert.IsNull(command.UserDomainName);
		}

		// ── --user / -u ───────────────────────────────────────────────────────────

		[TestMethod]
		public void UserOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--user", @"DOMAIN\john.doe",
				"--key", "showweeknumber",
				"--value", "true");

			Assert.AreEqual(@"DOMAIN\john.doe", command.UserDomainName);
			Assert.AreEqual("showweeknumber", command.Key);
			Assert.AreEqual("true", command.Value);
		}

		[TestMethod]
		public void UserOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"-u", "john.doe@contoso.com",
				"-k", "timezonecode",
				"-v", "85");

			Assert.AreEqual("john.doe@contoso.com", command.UserDomainName);
			Assert.AreEqual("timezonecode", command.Key);
			Assert.AreEqual("85", command.Value);
		}

		[TestMethod]
		public void UserShouldBeNullWhenOmitted()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--key", "paginglimit",
				"--value", "100");

			Assert.IsNull(command.UserDomainName);
			Assert.AreEqual("paginglimit", command.Key);
			Assert.AreEqual("100", command.Value);
		}
	}
}
