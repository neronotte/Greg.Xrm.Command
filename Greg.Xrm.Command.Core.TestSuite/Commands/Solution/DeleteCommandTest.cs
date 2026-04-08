using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class DeleteCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<DeleteCommand>(
				"solution", "delete",
				"--uniqueName", "MySolution_Unique"
			);

			Assert.AreEqual("MySolution_Unique", command.SolutionUniqueName);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<DeleteCommand>(
				"solution", "delete",
				"-un", "ShortUnique"
			);

			Assert.AreEqual("ShortUnique", command.SolutionUniqueName);
		}
	}
}
