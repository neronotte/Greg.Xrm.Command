using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		public void ParseWithNoArgumentsShouldReturnDefaults()
		{
			var command = Utility.TestParseCommand<ListCommand>("solution", "list");

			Assert.AreEqual(ListCommand.SolutionType.Both, command.Type);
			Assert.IsFalse(command.Hidden);
			Assert.AreEqual(ListCommand.OutputFormat.TableCompact, command.Format);
			Assert.AreEqual(ListCommand.OutputOrder.Name, command.OrderBy);
		}

		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"solution", "list",
				"--type", "Managed",
				"--hidden",
				"--format", "Json",
				"--orderby", "CreatedOn"
			);

			Assert.AreEqual(ListCommand.SolutionType.Managed, command.Type);
			Assert.IsTrue(command.Hidden);
			Assert.AreEqual(ListCommand.OutputFormat.Json, command.Format);
			Assert.AreEqual(ListCommand.OutputOrder.CreatedOn, command.OrderBy);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"solution", "list",
				"-t", "Unmanaged",
				"-hid",
				"-f", "Table",
				"-o", "Type"
			);

			Assert.AreEqual(ListCommand.SolutionType.Unmanaged, command.Type);
			Assert.IsTrue(command.Hidden);
			Assert.AreEqual(ListCommand.OutputFormat.Table, command.Format);
			Assert.AreEqual(ListCommand.OutputOrder.Type, command.OrderBy);
		}
	}
}
