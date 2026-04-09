namespace Greg.Xrm.Command.Commands.Data
{
	[TestClass]
	public class DataCommandsTest
	{
		[TestMethod]
		public void DataInitSchema_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<DataInitSchemaCommand>(
				"data", "init-schema-from-solution",
				"-s", "MySolution",
				"-o", "schema.yaml");

			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("schema.yaml", command.OutputPath);
			Assert.AreEqual("yaml", command.Format);
			Assert.IsNull(command.IncludeEntities);
			Assert.IsFalse(command.ExcludeRelationships);
			Assert.IsFalse(command.ExcludeOptionSets);
		}

		[TestMethod]
		public void DataInitSchema_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<DataInitSchemaCommand>(
				"data", "init-schema-from-solution",
				"-s", "MySolution",
				"-o", "schema.json",
				"-f", "json",
				"-i", "account", "contact",
				"--exclude-relationships",
				"--exclude-optionsets");

			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("schema.json", command.OutputPath);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual(2, command.IncludeEntities?.Length);
			Assert.IsTrue(command.ExcludeRelationships);
			Assert.IsTrue(command.ExcludeOptionSets);
		}

		[TestMethod]
		public void DataSeedMock_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<DataSeedMockCommand>(
				"data", "seed-mock",
				"-s", "schema.yaml",
				"-o", "mock-data.zip");

			Assert.AreEqual("schema.yaml", command.SchemaPath);
			Assert.AreEqual("mock-data.zip", command.OutputPath);
			Assert.AreEqual(100, command.RecordCount);
			Assert.AreEqual("random", command.Strategy);
			Assert.IsNull(command.RandomSeed);
			Assert.IsFalse(command.IncludeLookups);
		}

		[TestMethod]
		public void DataSeedMock_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<DataSeedMockCommand>(
				"data", "seed-mock",
				"-s", "schema.yaml",
				"-o", "mock-data.zip",
				"-c", "50",
				"--strategy", "sequential",
				"--seed", "12345",
				"--include-lookups");

			Assert.AreEqual("schema.yaml", command.SchemaPath);
			Assert.AreEqual("mock-data.zip", command.OutputPath);
			Assert.AreEqual(50, command.RecordCount);
			Assert.AreEqual("sequential", command.Strategy);
			Assert.AreEqual(12345, command.RandomSeed);
			Assert.IsTrue(command.IncludeLookups);
		}
	}
}
