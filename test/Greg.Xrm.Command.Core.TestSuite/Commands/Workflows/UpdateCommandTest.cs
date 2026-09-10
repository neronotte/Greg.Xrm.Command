namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class UpdateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<UpdateCommand>(
				"workflow", "update",
				"--name", "My Flow",
				"--file", "myflow.json");

			Assert.AreEqual("My Flow", command.Name);
			Assert.AreEqual("myflow.json", command.DefinitionFile);
		}

		[TestMethod]
		public void ParseWithIdAndShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<UpdateCommand>(
				"workflow", "update",
				"-i", "507db5fe-17f1-f011-8406-6045bd95f82d",
				"-f", "myflow.json");

			Assert.AreEqual(Guid.Parse("507db5fe-17f1-f011-8406-6045bd95f82d"), command.Id);
			Assert.AreEqual("myflow.json", command.DefinitionFile);
		}

		[TestMethod]
		public void ParseViaFlowAliasShouldWork()
		{
			var command = Utility.TestParseCommand<UpdateCommand>(
				"flow", "update",
				"--name", "My Flow",
				"--file", "myflow.json");

			Assert.AreEqual("My Flow", command.Name);
		}

		[TestMethod]
		public void ValidateShouldFailWhenNeitherNameNorIdIsGiven()
		{
			var command = new UpdateCommand { DefinitionFile = "myflow.json" };

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "--name");
		}

		[TestMethod]
		public void ValidateShouldFailWhenNameAndIdAreGivenTogether()
		{
			var command = new UpdateCommand { Name = "My Flow", Id = Guid.NewGuid(), DefinitionFile = "myflow.json" };

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "cannot be used together");
		}

		[TestMethod]
		public void ValidateShouldFailWhenFileIsMissing()
		{
			var command = new UpdateCommand { Name = "My Flow" };

			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "--file");
		}
	}
}
