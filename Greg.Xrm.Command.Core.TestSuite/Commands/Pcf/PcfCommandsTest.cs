namespace Greg.Xrm.Command.Commands.Pcf
{
	[TestClass]
	public class PcfCommandsTest
	{
		[TestMethod]
		public void PcfTest_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfTestCommand>(
				"pcf", "test");

			Assert.IsNull(command.Path);
			Assert.AreEqual("headless", command.Browser);
			Assert.AreEqual("spec", command.Reporter);
		}

		[TestMethod]
		public void PcfTest_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfTestCommand>(
				"pcf", "test",
				"-p", "C:\\pcf\\MyComponent",
				"-b", "chrome",
				"-r", "junit");

			Assert.AreEqual("C:\\pcf\\MyComponent", command.Path);
			Assert.AreEqual("chrome", command.Browser);
			Assert.AreEqual("junit", command.Reporter);
		}

		[TestMethod]
		public void PcfPublish_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfPublishCommand>(
				"pcf", "publish");

			Assert.IsNull(command.Path);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void PcfPublish_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfPublishCommand>(
				"pcf", "publish",
				"-p", "C:\\pcf\\MyComponent",
				"-s", "MySolution",
				"--dry-run");

			Assert.AreEqual("C:\\pcf\\MyComponent", command.Path);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void PcfVersionBump_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<PcfVersionBumpCommand>(
				"pcf", "version", "bump",
				"-t", "patch");

			Assert.IsNull(command.Path);
			Assert.AreEqual("patch", command.BumpType);
			Assert.IsNull(command.Message);
		}

		[TestMethod]
		public void PcfVersionBump_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfVersionBumpCommand>(
				"pcf", "version", "bump",
				"-p", "C:\\pcf\\MyComponent",
				"-t", "minor",
				"-m", "Added new feature");

			Assert.AreEqual("C:\\pcf\\MyComponent", command.Path);
			Assert.AreEqual("minor", command.BumpType);
			Assert.AreEqual("Added new feature", command.Message);
		}

		[TestMethod]
		public void PcfDependencyCheck_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<PcfDependencyCheckCommand>(
				"pcf", "dependency-check");

			Assert.IsNull(command.Path);
			Assert.IsNull(command.EnvironmentId);
			Assert.AreEqual("table", command.Format);
		}
	}
}
