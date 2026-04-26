namespace Greg.Xrm.Command.Commands.Plugin
{
	[TestClass]
	public class ListCommandTest
	{
		// ── --name / -n ───────────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso");

			Assert.AreEqual("Contoso", command.Name);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"-n", "Contoso");

			Assert.AreEqual("Contoso", command.Name);
		}

		// ── --level / -l ──────────────────────────────────────────────────────

		[TestMethod]
		public void DefaultLevelShouldBeNull()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso");

			Assert.IsNull(command.Level);
		}

		[TestMethod]
		public void LevelOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso",
				"--level", "Assembly");

			Assert.AreEqual(SearchLevel.Assembly, command.Level);
		}

		[TestMethod]
		public void LevelOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso",
				"-l", "Package");

			Assert.AreEqual(SearchLevel.Package, command.Level);
		}

		[TestMethod]
		[DataRow("Package", SearchLevel.Package)]
		[DataRow("Assembly", SearchLevel.Assembly)]
		[DataRow("Type", SearchLevel.Type)]
		[DataRow("Step", SearchLevel.Step)]
		public void AllSearchLevelValuesShouldBeParseable(string levelString, SearchLevel expected)
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "test",
				"--level", levelString);

			Assert.AreEqual(expected, command.Level);
		}

		// ── --solution / -s ───────────────────────────────────────────────────

		[TestMethod]
		public void DefaultSolutionShouldBeNull()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso");

			Assert.IsNull(command.SolutionName);
		}

		[TestMethod]
		public void SolutionOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso",
				"--solution", "MySolution");

			Assert.AreEqual("MySolution", command.SolutionName);
		}

		[TestMethod]
		public void SolutionOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "list",
				"--name", "Contoso",
				"-s", "MySolution");

			Assert.AreEqual("MySolution", command.SolutionName);
		}
	}
}
