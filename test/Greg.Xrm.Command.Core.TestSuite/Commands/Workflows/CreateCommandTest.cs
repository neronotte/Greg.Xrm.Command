namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class CreateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>(
				"workflow", "create",
				"--name", "My Flow",
				"--file", "C:\\temp\\myflow.json",
				"--solution", "My Solution");

			Assert.AreEqual("My Flow", command.Name);
			Assert.AreEqual("C:\\temp\\myflow.json", command.DefinitionFile);
			Assert.AreEqual("My Solution", command.SolutionName);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>(
				"workflow", "create",
				"-n", "My Flow",
				"-f", "myflow.json");

			Assert.AreEqual("My Flow", command.Name);
			Assert.AreEqual("myflow.json", command.DefinitionFile);
			Assert.IsNull(command.SolutionName);
		}

		[TestMethod]
		public void ParseViaFlowAliasShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>(
				"flow", "create",
				"--name", "My Flow",
				"--file", "myflow.json");

			Assert.AreEqual("My Flow", command.Name);
		}

		[TestMethod]
		public void ValidateShouldFailWhenNameIsMissing()
		{
			var command = new CreateCommand { DefinitionFile = "myflow.json" };

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "--name");
		}

		[TestMethod]
		public void ValidateShouldFailWhenFileIsMissing()
		{
			var command = new CreateCommand { Name = "My Flow" };

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "--file");
		}
	}
}
