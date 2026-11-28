using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		[DataRow("--name", "--solution", "--category", "--live", "--fail-on-mismatch")]
		[DataRow("-n", "-s", "-c", "-l", "-fm")]
		public void OptionsAreParsed(string name, string solution, string category, string live, string fail)
		{
			var command = Utility.TestParseCommand<ListCommand>("flow", "list",
				name, "Approval", solution, "test_solution", category, "ModernFlow", live, fail);

			Assert.AreEqual("Approval", command.SearchQuery);
			Assert.AreEqual("test_solution", command.SolutionName);
			Assert.AreEqual(Workflow.Category.ModernFlow, command.Category);
			Assert.IsTrue(command.Live);
			Assert.IsTrue(command.FailOnMismatch);
			Assert.AreEqual(0, command.Validate(new ValidationContext(command)).Count());
		}

		[TestMethod]
		public void LiveOptionsDefaultToFalse()
		{
			var command = Utility.TestParseCommand<ListCommand>("workflow", "list");

			Assert.AreEqual(string.Empty, command.SearchQuery);
			Assert.AreEqual(string.Empty, command.SolutionName);
			Assert.IsNull(command.Category);
			Assert.IsFalse(command.Live);
			Assert.IsFalse(command.FailOnMismatch);
		}

		[TestMethod]
		public void FailOnMismatchRequiresLive()
		{
			var command = Utility.TestParseCommand<ListCommand>("workflow", "list", "--fail-on-mismatch");

			StringAssert.Contains(command.Validate(new ValidationContext(command)).Single().ErrorMessage, "--live");
		}
	}
}
