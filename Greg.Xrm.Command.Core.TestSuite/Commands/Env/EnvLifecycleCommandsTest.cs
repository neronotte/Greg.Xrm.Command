namespace Greg.Xrm.Command.Commands.Env
{
	[TestClass]
	public class EnvLifecycleCommandsTest
	{
		[TestMethod]
		public void EnvResetParseWithRequiredIdShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvResetCommand>(
				"env", "reset",
				"--id", "env-123");

			Assert.AreEqual("env-123", command.EnvironmentId);
			Assert.AreEqual("full", command.ResetType);
			Assert.IsFalse(command.Force);
			Assert.IsFalse(command.Wait);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void EnvResetParseWithAllOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvResetCommand>(
				"env", "reset",
				"--id", "env-456",
				"-t", "customizations-only",
				"-f",
				"--wait",
				"--format", "json");

			Assert.AreEqual("env-456", command.EnvironmentId);
			Assert.AreEqual("customizations-only", command.ResetType);
			Assert.IsTrue(command.Force);
			Assert.IsTrue(command.Wait);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void EnvBackupParseWithRequiredIdShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvBackupCommand>(
				"env", "backup",
				"--id", "env-789");

			Assert.AreEqual("env-789", command.EnvironmentId);
			Assert.IsNull(command.BackupName);
			Assert.IsFalse(command.IncludeData);
			Assert.IsFalse(command.Wait);
		}

		[TestMethod]
		public void EnvBackupParseWithDataAndNameShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvBackupCommand>(
				"env", "backup",
				"--id", "env-100",
				"-n", "pre-deploy-backup",
				"--include-data",
				"--wait");

			Assert.AreEqual("env-100", command.EnvironmentId);
			Assert.AreEqual("pre-deploy-backup", command.BackupName);
			Assert.IsTrue(command.IncludeData);
			Assert.IsTrue(command.Wait);
		}

		[TestMethod]
		public void EnvRestoreParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvRestoreCommand>(
				"env", "restore",
				"--id", "env-200",
				"--backup-id", "backup-123");

			Assert.AreEqual("env-200", command.EnvironmentId);
			Assert.AreEqual("backup-123", command.BackupId);
			Assert.IsFalse(command.Force);
			Assert.IsFalse(command.Wait);
		}

		[TestMethod]
		public void EnvRestoreParseWithForceShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<EnvRestoreCommand>(
				"env", "restore",
				"--id", "env-300",
				"--backup-id", "backup-456",
				"-f",
				"--wait");

			Assert.AreEqual("env-300", command.EnvironmentId);
			Assert.AreEqual("backup-456", command.BackupId);
			Assert.IsTrue(command.Force);
			Assert.IsTrue(command.Wait);
		}
	}
}
