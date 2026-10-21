namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class SetRibbonDiffCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesEveryOption(bool shortNames)
		{
			var command = Utility.TestParseCommand<SetRibbonDiffCommand>("ribbon", "setdiff",
				shortNames ? "-f" : "--file", "input.xml",
				shortNames ? "-b" : "--backup", "backup.xml",
				shortNames ? "-t" : "--table", "account",
				shortNames ? "-s" : "--solution", "RibbonDiff");
			Assert.AreEqual("input.xml", command.FileName);
			Assert.AreEqual("backup.xml", command.BackupFile);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("RibbonDiff", command.SolutionName);
		}

		[TestMethod]
		public void DefaultsToApplicationRibbonAndCurrentSolution()
		{
			var command = Utility.TestParseCommand<SetRibbonDiffCommand>("ribbon", "setdiff", "--file", "input.xml");
			Assert.AreEqual(string.Empty, command.TableName);
			Assert.IsNull(command.SolutionName);
			Assert.IsNull(command.BackupFile);
		}
	}
}
