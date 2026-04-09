namespace Greg.Xrm.Command.Commands.Env
{
	[TestClass]
	public class EnvCommandsTest
	{
		[TestMethod]
		public void EnvCreate_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCreateCommand>(
				"env", "create",
				"-n", "Dev Environment");

			Assert.AreEqual("Dev Environment", command.Name);
			Assert.AreEqual("Sandbox", command.Type);
			Assert.IsNull(command.Region);
			Assert.AreEqual("USD", command.Currency);
			Assert.AreEqual("en-US", command.Language);
			Assert.IsNull(command.SecurityGroupId);
			Assert.IsFalse(command.Wait);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void EnvCreate_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCreateCommand>(
				"env", "create",
				"-n", "Prod Environment",
				"-t", "Production",
				"-r", "europe",
				"--currency", "EUR",
				"--language", "en-GB",
				"--security-group", "group-id-123",
				"--wait",
				"-f", "json");

			Assert.AreEqual("Prod Environment", command.Name);
			Assert.AreEqual("Production", command.Type);
			Assert.AreEqual("europe", command.Region);
			Assert.AreEqual("EUR", command.Currency);
			Assert.AreEqual("en-GB", command.Language);
			Assert.AreEqual("group-id-123", command.SecurityGroupId);
			Assert.IsTrue(command.Wait);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void EnvClone_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCloneCommand>(
				"env", "clone",
				"-s", "env-prod",
				"-n", "env-sandbox");

			Assert.AreEqual("env-prod", command.SourceEnvironmentId);
			Assert.AreEqual("env-sandbox", command.Name);
			Assert.AreEqual("schema-only", command.Mode);
			Assert.IsNull(command.Tables);
			Assert.IsFalse(command.Wait);
		}

		[TestMethod]
		public void EnvClone_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCloneCommand>(
				"env", "clone",
				"-s", "env-prod",
				"-n", "env-test",
				"-m", "selective",
				"-t", "account", "contact",
				"--wait");

			Assert.AreEqual("env-prod", command.SourceEnvironmentId);
			Assert.AreEqual("env-test", command.Name);
			Assert.AreEqual("selective", command.Mode);
			Assert.AreEqual(2, command.Tables?.Length);
			Assert.IsTrue(command.Wait);
		}

		[TestMethod]
		public void EnvCapacityReport_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCapacityReportCommand>(
				"env", "capacity", "report");

			Assert.IsNull(command.EnvironmentId);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void EnvCapacityReport_ParseWithFilterShouldWork()
		{
			var command = Utility.TestParseCommand<EnvCapacityReportCommand>(
				"env", "capacity", "report",
				"-e", "env-dev",
				"-f", "json");

			Assert.AreEqual("env-dev", command.EnvironmentId);
			Assert.AreEqual("json", command.Format);
		}
	}
}
