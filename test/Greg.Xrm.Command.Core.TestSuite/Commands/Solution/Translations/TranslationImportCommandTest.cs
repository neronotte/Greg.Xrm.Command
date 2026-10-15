namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[TestClass]
	public class TranslationImportCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslationImportCommand>(
				"solution", "translation", "import",
				"--path", "C:/temp/import.zip",
				"--repack",
				"--nowait");

			Assert.AreEqual("C:/temp/import.zip", command.Path);
			Assert.IsTrue(command.Repack);
			Assert.IsTrue(command.NoWait);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslationImportCommand>(
				"solution", "translation", "import",
				"-p", "C:/temp/import.zip",
				"-r",
				"-nw");

			Assert.AreEqual("C:/temp/import.zip", command.Path);
			Assert.IsTrue(command.Repack);
			Assert.IsTrue(command.NoWait);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<TranslationImportCommand>(
				"solution", "translation", "import",
				"--path", "C:/temp/import.zip");

			Assert.AreEqual("C:/temp/import.zip", command.Path);
			Assert.IsFalse(command.Repack);
			Assert.IsFalse(command.NoWait);
		}
	}
}
