namespace Greg.Xrm.Command.Commands.Connector
{
	[TestClass]
	public class ConnectorCommandsTest
	{
		[TestMethod]
		public void ConnectorImport_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorImportCommand>(
				"connector", "import",
				"-f", "connector.json");

			Assert.AreEqual("connector.json", command.FilePath);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void ConnectorImport_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorImportCommand>(
				"connector", "import",
				"-f", "connector.json",
				"-s", "MySolution",
				"--dry-run");

			Assert.AreEqual("connector.json", command.FilePath);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void ConnectorExport_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorExportCommand>(
				"connector", "export",
				"-n", "my-connector",
				"-o", "export.json");

			Assert.AreEqual("my-connector", command.ConnectorName);
			Assert.AreEqual("export.json", command.OutputPath);
		}

		[TestMethod]
		public void ConnectorTest_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorTestCommand>(
				"connector", "test",
				"-n", "my-connector",
				"-o", "GetItems");

			Assert.AreEqual("my-connector", command.ConnectorName);
			Assert.AreEqual("GetItems", command.OperationName);
			Assert.IsNull(command.PayloadPath);
		}

		[TestMethod]
		public void ConnectorValidate_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorValidateCommand>(
				"connector", "validate",
				"-f", "connector.json");

			Assert.AreEqual("connector.json", command.FilePath);
			Assert.IsFalse(command.Strict);
		}

		[TestMethod]
		public void ConnectorValidate_ParseWithStrictShouldWork()
		{
			var command = Utility.TestParseCommand<ConnectorValidateCommand>(
				"connector", "validate",
				"-f", "connector.json",
				"--strict");

			Assert.IsTrue(command.Strict);
		}
	}
}
