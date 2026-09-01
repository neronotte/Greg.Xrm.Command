using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class GetCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<GetCommand>(
				"workflow", "get",
				"--name", "My Flow",
				"--solution", "MySolution",
				"--output", "C:\\temp\\myflow.json");

			Assert.AreEqual("My Flow", command.Name);
			Assert.AreEqual("MySolution", command.SolutionName);
			Assert.AreEqual("C:\\temp\\myflow.json", command.OutputFile);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<GetCommand>(
				"workflow", "get",
				"-n", "My Flow");

			Assert.AreEqual("My Flow", command.Name);
			Assert.IsNull(command.SolutionName);
			Assert.IsNull(command.OutputFile);
		}

		[TestMethod]
		public void ParseWithFlowAliasShouldWork()
		{
			var command = Utility.TestParseCommand<GetCommand>(
				"flow", "get",
				"-n", "My Flow");

			Assert.AreEqual("My Flow", command.Name);
		}

		[TestMethod]
		public void ParseWithIdShouldWork()
		{
			var command = Utility.TestParseCommand<GetCommand>(
				"workflow", "get",
				"--id", "507db5fe-17f1-f011-8406-6045bd95f82d");

			Assert.AreEqual(Guid.Parse("507db5fe-17f1-f011-8406-6045bd95f82d"), command.Id);
			Assert.AreEqual(string.Empty, command.Name);
		}

		[TestMethod]
		public void ValidateShouldFailWhenNeitherNameNorIdIsProvided()
		{
			var command = new GetCommand();

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void ValidateShouldFailWhenNameAndIdAreUsedTogether()
		{
			var command = new GetCommand { Name = "My Flow", Id = Guid.NewGuid() };

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void ValidateShouldPassWithNameOnly()
		{
			var command = new GetCommand { Name = "My Flow" };

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(0, results.Count);
		}
	}
}
