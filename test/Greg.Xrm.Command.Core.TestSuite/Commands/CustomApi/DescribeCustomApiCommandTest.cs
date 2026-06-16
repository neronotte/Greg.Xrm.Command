namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class DescribeCustomApiCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "--unique-name", "nn_GregSum");

			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum");

			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum");

			Assert.IsNotNull(command.UniqueName);
			Assert.IsNull(command.GenerateInputFile);
			Assert.IsNull(command.GenerateSchemaFile);
		}

		[TestMethod]
		public void ParseGenerateInputFile_LongNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum", "--generate-input-file", "input.json");

			Assert.AreEqual("input.json", command.GenerateInputFile);
		}

		[TestMethod]
		public void ParseGenerateInputFile_ShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum", "-gif", "input.json");

			Assert.AreEqual("input.json", command.GenerateInputFile);
		}

		[TestMethod]
		public void ParseGenerateSchemaFile_LongNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum", "--generate-schema-file", "schema.json");

			Assert.AreEqual("schema.json", command.GenerateSchemaFile);
		}

		[TestMethod]
		public void ParseGenerateSchemaFile_ShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum", "-gsf", "schema.json");

			Assert.AreEqual("schema.json", command.GenerateSchemaFile);
		}

		[TestMethod]
		public void ParseBothFileOptions_ShouldWorkTogether()
		{
			var command = Utility.TestParseCommand<DescribeCustomApiCommand>(
				"customapi", "describe", "-n", "nn_GregSum",
				"-gif", "input.json", "-gsf", "schema.json");

			Assert.AreEqual("input.json", command.GenerateInputFile);
			Assert.AreEqual("schema.json", command.GenerateSchemaFile);
		}
	}
}
