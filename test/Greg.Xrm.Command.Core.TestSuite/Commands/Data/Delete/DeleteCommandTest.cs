using Greg.Xrm.Command.Commands.Data.Delete;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Data.Delete
{
	[TestClass]
	public class DeleteCommandTest
	{
		#region Parsing Tests

		[TestMethod]
		public void Parse_WithTableLongOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<DeleteCommand>("data", "delete", "--table", "contact", "--id", guid.ToString());
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithTableShortOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<DeleteCommand>("data", "delete", "-t", "contact", "--id", guid.ToString());
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithIdOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<DeleteCommand>("data", "delete", "-t", "contact", "--id", guid.ToString());
			Assert.AreEqual(guid, command.Id);
		}

		[TestMethod]
		public void Parse_WithDryRunOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<DeleteCommand>("data", "delete", "-t", "contact", "--id", guid.ToString(), "--dry-run");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_DryRunShouldBeFalse()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<DeleteCommand>("data", "delete", "-t", "contact", "--id", guid.ToString());
			Assert.IsFalse(command.DryRun);
		}

		#endregion

		#region Validation Tests

		[TestMethod]
		public void Validate_WhenIdIsEmpty_ShouldReturnError()
		{
			var command = new DeleteCommand
			{
				Table = "contact",
				Id = Guid.Empty
			};
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("non-empty GUID")));
		}

		[TestMethod]
		public void Validate_WhenValid_ShouldPass()
		{
			var command = new DeleteCommand
			{
				Table = "contact",
				Id = Guid.NewGuid()
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		#endregion

		#region Helper Methods

		private static List<ValidationResult> Validate(DeleteCommand command)
		{
			var context = new ValidationContext(command);
			return command.Validate(context).ToList();
		}

		#endregion
	}
}
