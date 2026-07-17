using Greg.Xrm.Command.Commands.Data.Create;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Data.Create
{
	[TestClass]
	public class CreateCommandTest
	{
		#region Parsing Tests

		[TestMethod]
		public void Parse_WithTableLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "--table", "contact", "--plain", "firstname=Mario");
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithTableShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario");
			Assert.AreEqual("contact", command.Table);
		}

		[TestMethod]
		public void Parse_WithPlainLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario");
			Assert.AreEqual("firstname=Mario", command.Plain);
		}

		[TestMethod]
		public void Parse_WithPlainShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "-p", "firstname=Mario");
			Assert.AreEqual("firstname=Mario", command.Plain);
		}

		[TestMethod]
		public void Parse_WithJsonLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--json", "{\"firstname\":\"Mario\"}");
			Assert.AreEqual("{\"firstname\":\"Mario\"}", command.Json);
		}

		[TestMethod]
		public void Parse_WithJsonShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "-j", "{\"name\":\"Test\"}");
			Assert.AreEqual("{\"name\":\"Test\"}", command.Json);
		}

		[TestMethod]
		public void Parse_WithFileLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--file", "C:\\data.json");
			Assert.AreEqual("C:\\data.json", command.File);
		}

		[TestMethod]
		public void Parse_WithFileShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "-f", "C:\\data.json");
			Assert.AreEqual("C:\\data.json", command.File);
		}

		[TestMethod]
		public void Parse_WithIdOption_ShouldWork()
		{
			var guid = Guid.NewGuid();
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario", "--id", guid.ToString());
			Assert.AreEqual(guid, command.Id);
		}

		[TestMethod]
		public void Parse_WithReturnLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario", "--return", "firstname,lastname");
			Assert.AreEqual("firstname,lastname", command.Return);
		}

		[TestMethod]
		public void Parse_WithReturnShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario", "-r", "firstname");
			Assert.AreEqual("firstname", command.Return);
		}

		[TestMethod]
		public void Parse_WithDryRunLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario", "--dry-run");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_WithDryRunShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario", "-dr");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_DryRunShouldBeFalse()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario");
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_IdShouldBeNull()
		{
			var command = Utility.TestParseCommand<CreateCommand>("data", "create", "-t", "contact", "--plain", "firstname=Mario");
			Assert.IsNull(command.Id);
		}

		#endregion

		#region Validation Tests

		[TestMethod]
		public void Validate_WhenNeitherPlainNorJsonNorFile_ShouldReturnError()
		{
			var command = new CreateCommand { Table = "contact" };
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Exactly one")));
		}

		[TestMethod]
		public void Validate_WhenTwoPayloadSourcesProvided_ShouldReturnError()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario",
				Json = "{\"firstname\":\"Mario\"}"
			};
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Only one")));
		}

		[TestMethod]
		public void Validate_WhenAllThreePayloadSourcesProvided_ShouldReturnError()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario",
				Json = "{\"firstname\":\"Mario\"}",
				File = "C:\\data.json"
			};
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Only one")));
		}

		[TestMethod]
		public void Validate_WhenFileDoesNotExist_ShouldReturnError()
		{
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
			var command = new CreateCommand
			{
				Table = "contact",
				File = nonExistentFile
			};
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("does not exist")));
		}

		[TestMethod]
		public void Validate_WhenPlainProvided_ShouldPass()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario"
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_WhenJsonProvided_ShouldPass()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Json = "{\"firstname\":\"Mario\"}"
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_WhenFileExistsAndProvided_ShouldPass()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				var command = new CreateCommand
				{
					Table = "contact",
					File = tempFile
				};
				var results = Validate(command);

				Assert.AreEqual(0, results.Count);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		#endregion

		#region Helper Methods

		private static List<ValidationResult> Validate(CreateCommand command)
		{
			var context = new ValidationContext(command);
			return command.Validate(context).ToList();
		}

		#endregion
	}
}
