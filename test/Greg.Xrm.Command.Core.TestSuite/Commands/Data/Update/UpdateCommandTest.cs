using Greg.Xrm.Command.Commands.Data.Update;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Data.Update
{
	[TestClass]
	public class UpdateCommandTest
	{
		#region Parsing Tests

		[TestMethod]
		public void Parse_WithTableLongOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "--table", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario");
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithTableShortOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario");
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithIdOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario");
			Assert.AreEqual(guid, command.Id);
		}

		[TestMethod]
		public void Parse_WithPlainOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario");
			Assert.AreEqual("firstname=Mario", command.Plain);
		}

		[TestMethod]
		public void Parse_WithPlainShortOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "-p", "firstname=Mario");
			Assert.AreEqual("firstname=Mario", command.Plain);
		}

		[TestMethod]
		public void Parse_WithJsonOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--json", "{\"firstname\":\"Mario\"}");
			Assert.AreEqual("{\"firstname\":\"Mario\"}", command.Json);
		}

		[TestMethod]
		public void Parse_WithFileOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--file", "C:\\data.json");
			Assert.AreEqual("C:\\data.json", command.File);
		}

		[TestMethod]
		public void Parse_WithReturnOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario", "--return", "firstname,lastname");
			Assert.AreEqual("firstname,lastname", command.Return);
		}

		[TestMethod]
		public void Parse_WithDryRunOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario", "--dry-run");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_DryRunShouldBeFalse()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<UpdateCommand>("data", "update", "-t", "contact", "--id", guid.ToString(), "--plain", "firstname=Mario");
			Assert.IsFalse(command.DryRun);
		}

		#endregion

		#region Validation Tests

		[TestMethod]
		public void Validate_WhenNeitherPlainNorJsonNorFile_ShouldReturnError()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid()
			};
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Exactly one")));
		}

		[TestMethod]
		public void Validate_WhenTwoPayloadSourcesProvided_ShouldReturnError()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				Plain = "firstname=Mario",
				Json = "{\"firstname\":\"Mario\"}"
			};
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Only one")));
		}

		[TestMethod]
		public void Validate_WhenFileDoesNotExist_ShouldReturnError()
		{
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				File = nonExistentFile
			};
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("does not exist")));
		}

		[TestMethod]
		public void Validate_WhenPlainProvided_ShouldPass()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				Plain = "firstname=Mario"
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_WhenJsonProvided_ShouldPass()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				Json = "{\"firstname\":\"Mario\"}"
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		#endregion

		#region Helper Methods

		private static List<ValidationResult> Validate(UpdateCommand command)
		{
			var context = new ValidationContext(command);
			return command.Validate(context).ToList();
		}

		#endregion
	}
}
