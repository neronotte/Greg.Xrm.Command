namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class SolutionDiffCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionDiffCommand>(
				"solution", "diff",
				"-s", "DevSolution",
				"-t", "ProdSolution");

			Assert.AreEqual("DevSolution", command.Source);
			Assert.AreEqual("ProdSolution", command.Target);
			Assert.AreEqual("solution", command.DiffType);
			Assert.AreEqual("table", command.Format);
			Assert.IsNull(command.ComponentType);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionDiffCommand>(
				"solution", "diff",
				"-s", "DevSolution",
				"-t", "ProdSolution",
				"--type", "environment",
				"--component-type", "entity",
				"-f", "json");

			Assert.AreEqual("DevSolution", command.Source);
			Assert.AreEqual("ProdSolution", command.Target);
			Assert.AreEqual("environment", command.DiffType);
			Assert.AreEqual("entity", command.ComponentType);
			Assert.AreEqual("json", command.Format);
		}
	}
}
