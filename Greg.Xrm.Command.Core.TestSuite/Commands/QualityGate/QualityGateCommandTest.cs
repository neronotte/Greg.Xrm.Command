namespace Greg.Xrm.Command.Commands.QualityGate
{
	[TestClass]
	public class QualityGateCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<QualityGateCommand>(
				"quality", "gate");

			Assert.IsNull(command.InputPath);
			Assert.AreEqual("High", command.FailOnSeverity);
			Assert.AreEqual("table", command.Format);
			Assert.IsNull(command.SolutionUniqueName);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<QualityGateCommand>(
				"quality", "gate",
				"-i", "results.zip",
				"--fail-on", "Medium",
				"-f", "json",
				"-s", "MySolution");

			Assert.AreEqual("results.zip", command.InputPath);
			Assert.AreEqual("Medium", command.FailOnSeverity);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
		}
	}
}
