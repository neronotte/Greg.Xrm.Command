namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class SolutionComponentMoveCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionComponentMoveCommand>(
				"solution", "component-move",
				"-c", "account",
				"-t", "entity",
				"--from", "DevSolution",
				"--to", "ProdSolution");

			Assert.AreEqual("account", command.ComponentName);
			Assert.AreEqual("entity", command.ComponentType);
			Assert.AreEqual("DevSolution", command.FromSolution);
			Assert.AreEqual("ProdSolution", command.ToSolution);
			Assert.IsFalse(command.IncludeDependencies);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionComponentMoveCommand>(
				"solution", "component-move",
				"-c", "account",
				"-t", "entity",
				"--from", "DevSolution",
				"--to", "ProdSolution",
				"-d",
				"--dry-run");

			Assert.IsTrue(command.IncludeDependencies);
			Assert.IsTrue(command.DryRun);
		}
	}
}
