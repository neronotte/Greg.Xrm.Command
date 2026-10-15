namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[TestClass]
	public class TranslationExportCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslationExportCommand>(
				"solution", "translation", "export",
				"--solution", "MySolution",
				"--path", "C:/temp/export.zip",
				"--unpack");

			Assert.AreEqual("MySolution", command.SolutionName);
			Assert.AreEqual("C:/temp/export.zip", command.Path);
			Assert.IsTrue(command.Unpack);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslationExportCommand>(
				"solution", "translation", "export",
				"-s", "MySolution",
				"-p", "C:/temp/export.zip",
				"-u");

			Assert.AreEqual("MySolution", command.SolutionName);
			Assert.AreEqual("C:/temp/export.zip", command.Path);
			Assert.IsTrue(command.Unpack);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<TranslationExportCommand>(
				"solution", "translation", "export");

			Assert.IsNull(command.SolutionName);
			Assert.IsNull(command.Path);
			Assert.IsFalse(command.Unpack);
		}
	}
}
