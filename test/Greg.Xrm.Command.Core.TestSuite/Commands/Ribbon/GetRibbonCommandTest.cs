namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class GetRibbonCommandTest
	{
		// ── no options — valid because everything is optional ─────────────────

		[TestMethod]
		public void ParseWithNoOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get");

			Assert.AreEqual(string.Empty, command.EntityName);
			Assert.AreEqual(string.Empty, command.FileName);
			Assert.IsFalse(command.AutoRun);
		}

		// ── --table / -t ──────────────────────────────────────────────────────

		[TestMethod]
		public void TableOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"--table", "account");

			Assert.AreEqual("account", command.EntityName);
		}

		[TestMethod]
		public void TableOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"-t", "account");

			Assert.AreEqual("account", command.EntityName);
		}

		// ── --output / -o ─────────────────────────────────────────────────────

		[TestMethod]
		public void OutputOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"--output", "ribbon.xml");

			Assert.AreEqual("ribbon.xml", command.FileName);
		}

		[TestMethod]
		public void OutputOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"-o", @"C:\temp\ribbon.xml");

			Assert.AreEqual(@"C:\temp\ribbon.xml", command.FileName);
		}

		// ── --autorun / -r ────────────────────────────────────────────────────

		[TestMethod]
		public void AutoRunShouldDefaultToFalse()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"--table", "account");

			Assert.IsFalse(command.AutoRun);
		}

		[TestMethod]
		public void AutoRunWithLongNameShouldBeTrueWhenProvided()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"--table", "account",
				"--autorun");

			Assert.IsTrue(command.AutoRun);
		}

		[TestMethod]
		public void AutoRunWithShortNameShouldBeTrueWhenProvided()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"-t", "account",
				"-r");

			Assert.IsTrue(command.AutoRun);
		}

		// ── combined ──────────────────────────────────────────────────────────

		[TestMethod]
		public void AllOptionsTogetherShouldWork()
		{
			var command = Utility.TestParseCommand<GetRibbonCommand>(
				"ribbon", "get",
				"-t", "account",
				"-o", "ribbon.xml",
				"-r");

			Assert.AreEqual("account", command.EntityName);
			Assert.AreEqual("ribbon.xml", command.FileName);
			Assert.IsTrue(command.AutoRun);
		}
	}
}
