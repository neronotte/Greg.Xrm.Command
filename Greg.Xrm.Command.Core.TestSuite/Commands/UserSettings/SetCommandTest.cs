namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class SetCommandTest
	{
		// ── single --key / --value (long names) ───────────────────────────────────

		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--key", "uilanguageid",
				"--value", "1040");

			Assert.AreEqual(1, command.Keys.Count);
			Assert.AreEqual("uilanguageid", command.Keys[0]);
			Assert.AreEqual(1, command.Values.Count);
			Assert.AreEqual("1040", command.Values[0]);
			Assert.IsNull(command.UserDomainName);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"-k", "timeformatcode",
				"-v", "1");

			Assert.AreEqual(1, command.Keys.Count);
			Assert.AreEqual("timeformatcode", command.Keys[0]);
			Assert.AreEqual(1, command.Values.Count);
			Assert.AreEqual("1", command.Values[0]);
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
			CollectionAssert.AreEqual(new[] { "showweeknumber" }, command.Keys);
			CollectionAssert.AreEqual(new[] { "true" }, command.Values);
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
			CollectionAssert.AreEqual(new[] { "timezonecode" }, command.Keys);
			CollectionAssert.AreEqual(new[] { "85" }, command.Values);
		}

		[TestMethod]
		public void UserShouldBeNullWhenOmitted()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--key", "paginglimit",
				"--value", "100");

			Assert.IsNull(command.UserDomainName);
			CollectionAssert.AreEqual(new[] { "paginglimit" }, command.Keys);
			CollectionAssert.AreEqual(new[] { "100" }, command.Values);
		}

		// ── multiple --key / --value pairs ────────────────────────────────────────

		[TestMethod]
		public void ParseMultiplePairsShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--key", "uilanguageid",
				"--value", "1040",
				"--key", "helplanguageid",
				"--value", "1040",
				"--key", "localeid",
				"--value", "1040");

			Assert.AreEqual(3, command.Keys.Count);
			CollectionAssert.AreEqual(new[] { "uilanguageid", "helplanguageid", "localeid" }, command.Keys);
			Assert.AreEqual(3, command.Values.Count);
			CollectionAssert.AreEqual(new[] { "1040", "1040", "1040" }, command.Values);
		}

		[TestMethod]
		public void ParseMultiplePairsWithUserShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--user", @"DOMAIN\alice",
				"--key", "paginglimit",
				"--value", "250",
				"--key", "showweeknumber",
				"--value", "true");

			Assert.AreEqual(@"DOMAIN\alice", command.UserDomainName);
			CollectionAssert.AreEqual(new[] { "paginglimit", "showweeknumber" }, command.Keys);
			CollectionAssert.AreEqual(new[] { "250", "true" }, command.Values);
		}
	}
}
