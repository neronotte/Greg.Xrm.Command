namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class GetRibbonDiffCommandTest
	{
		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ParsesEveryOption(bool shortNames)
		{
			var command = Utility.TestParseCommand<GetRibbonDiffCommand>("ribbon", "getdiff",
				shortNames ? "-o" : "--output", "output.xml",
				shortNames ? "-t" : "--table", "account",
				shortNames ? "-s" : "--solution", "RibbonDiff");
			Assert.AreEqual("output.xml", command.FileName);
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("RibbonDiff", command.SolutionName);
		}

		[TestMethod]
		public void DefaultsToApplicationRibbonAndCurrentSolution()
		{
			var command = Utility.TestParseCommand<GetRibbonDiffCommand>("ribbon", "getdiff");
			Assert.AreEqual(string.Empty, command.TableName);
			Assert.IsNull(command.SolutionName);
			Assert.AreEqual(string.Empty, command.FileName);
		}
	}
}
