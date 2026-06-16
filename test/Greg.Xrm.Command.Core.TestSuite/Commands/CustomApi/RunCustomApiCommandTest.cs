namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RunCustomApiCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<RunCustomApiCommand>(
				"customapi", "run", "--unique-name", "nn_GregSum");

			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<RunCustomApiCommand>(
				"customapi", "run", "-n", "nn_GregSum");

			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseInlineInputShouldWork()
		{
			var command = Utility.TestParseCommand<RunCustomApiCommand>(
				"customapi", "run", "-n", "nn_GregSum", "-i", "{\"Addend1\":5}");

			Assert.AreEqual("{\"Addend1\":5}", command.Input);
			Assert.IsNull(command.InputFile);
		}

		[TestMethod]
		public void ParseInputFileShouldWork()
		{
			var command = Utility.TestParseCommand<RunCustomApiCommand>(
				"customapi", "run", "-n", "nn_GregSum", "--input-file", "params.json");

			Assert.AreEqual("params.json", command.InputFile);
			Assert.IsNull(command.Input);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<RunCustomApiCommand>(
				"customapi", "run", "-n", "nn_GregSum");

			Assert.IsNull(command.Input);
			Assert.IsNull(command.InputFile);
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenBothInputAndInputFileProvided()
		{
			var command = new RunCustomApiCommand
			{
				UniqueName  = "nn_GregSum",
				Input       = "{\"X\":1}",
				InputFile   = "params.json"
			};

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "mutually exclusive");
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenOnlyInputProvided()
		{
			var command = new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{}" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenNeitherInputProvided()
		{
			var command = new RunCustomApiCommand { UniqueName = "nn_GregSum" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}
	}
}
