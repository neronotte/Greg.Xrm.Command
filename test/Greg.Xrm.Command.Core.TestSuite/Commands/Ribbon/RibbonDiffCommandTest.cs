namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class RibbonDiffCommandTest
	{
		[TestMethod]
		public void GetDiffParsesTableOutputAndSolution()
		{
			var command = Utility.TestParseCommand<GetRibbonDiffCommand>(
				"ribbon", "getdiff", "--table", "account", "--output", "account.xml", "--solution", "Example");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("account.xml", command.FileName);
			Assert.AreEqual("Example", command.SolutionName);
		}

		[TestMethod]
		public void SetDiffParsesFileAndBackup()
		{
			var command = Utility.TestParseCommand<SetRibbonDiffCommand>(
				"ribbon", "setdiff", "--table", "account", "--file", "account.xml", "--backup", "backup.xml");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("account.xml", command.FileName);
			Assert.AreEqual("backup.xml", command.BackupFile);
		}

		[TestMethod]
		public void SetDiffParsesShortBackupOption()
		{
			var command = Utility.TestParseCommand<SetRibbonDiffCommand>(
				"ribbon", "setdiff", "-f", "account.xml", "-b", "account.backup.xml");

			Assert.AreEqual("account.backup.xml", command.BackupFile);
		}
	}
}
