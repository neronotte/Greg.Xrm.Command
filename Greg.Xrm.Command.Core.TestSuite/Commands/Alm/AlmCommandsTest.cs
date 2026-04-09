namespace Greg.Xrm.Command.Commands.Alm
{
	[TestClass]
	public class AlmCommandsTest
	{
		[TestMethod]
		public void PipelineCreate_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AlmPipelineCreateCommand>(
				"alm", "pipeline", "create",
				"-n", "DevToProd");

			Assert.AreEqual("DevToProd", command.Name);
			Assert.AreEqual("Deployment", command.Type);
			Assert.IsNull(command.SourceEnvironmentId);
			Assert.IsNull(command.TargetEnvironmentId);
		}

		[TestMethod]
		public void PipelineCreate_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<AlmPipelineCreateCommand>(
				"alm", "pipeline", "create",
				"-n", "DevToProd",
				"-t", "Validation",
				"--source-env", "env-dev",
				"--target-env", "env-prod");

			Assert.AreEqual("DevToProd", command.Name);
			Assert.AreEqual("Validation", command.Type);
			Assert.AreEqual("env-dev", command.SourceEnvironmentId);
			Assert.AreEqual("env-prod", command.TargetEnvironmentId);
		}

		[TestMethod]
		public void PipelineRun_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AlmPipelineRunCommand>(
				"alm", "pipeline", "run",
				"--id", "pipeline-123");

			Assert.AreEqual("pipeline-123", command.PipelineId);
			Assert.IsNull(command.Stage);
			Assert.IsFalse(command.Wait);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void PipelineRun_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<AlmPipelineRunCommand>(
				"alm", "pipeline", "run",
				"--id", "pipeline-123",
				"-s", "deploy",
				"--wait",
				"-f", "json");

			Assert.AreEqual("pipeline-123", command.PipelineId);
			Assert.AreEqual("deploy", command.Stage);
			Assert.IsTrue(command.Wait);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void EnvVarSync_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AlmEnvVarSyncCommand>(
				"alm", "env-var", "sync",
				"-s", "env-dev",
				"-t", "env-prod");

			Assert.AreEqual("env-dev", command.SourceEnvironmentId);
			Assert.AreEqual("env-prod", command.TargetEnvironmentId);
			Assert.IsNull(command.MappingFile);
			Assert.IsFalse(command.DryRun);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void EnvVarSync_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<AlmEnvVarSyncCommand>(
				"alm", "env-var", "sync",
				"-s", "env-dev",
				"-t", "env-prod",
				"-m", "mapping.yaml",
				"--dry-run",
				"-f", "json");

			Assert.AreEqual("env-dev", command.SourceEnvironmentId);
			Assert.AreEqual("env-prod", command.TargetEnvironmentId);
			Assert.AreEqual("mapping.yaml", command.MappingFile);
			Assert.IsTrue(command.DryRun);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void EnvDiff_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AlmEnvDiffCommand>(
				"alm", "env", "diff",
				"--env-a", "env-dev",
				"--env-b", "env-prod");

			Assert.AreEqual("env-dev", command.EnvA);
			Assert.AreEqual("env-prod", command.EnvB);
			Assert.AreEqual("all", command.Scope);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void SolutionLayer_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionLayerCommand>(
				"solution", "layer",
				"-s", "MySolution");

			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsFalse(command.Show);
			Assert.IsNull(command.PinVersion);
			Assert.IsFalse(command.CheckDependencies);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void SolutionLayer_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<SolutionLayerCommand>(
				"solution", "layer",
				"-s", "MySolution",
				"--show",
				"--pin-version", "1.2.0",
				"--check-deps",
				"-f", "json");

			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.Show);
			Assert.AreEqual("1.2.0", command.PinVersion);
			Assert.IsTrue(command.CheckDependencies);
			Assert.AreEqual("json", command.Format);
		}
	}
}
