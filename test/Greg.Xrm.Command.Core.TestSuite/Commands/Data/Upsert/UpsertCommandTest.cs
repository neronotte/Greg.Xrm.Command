using Greg.Xrm.Command.Commands.Data.Upsert;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Data.Upsert
{
	[TestClass]
	public class UpsertCommandTest
	{
		#region Parsing Tests

		[TestMethod]
		public void Parse_WithTableLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "--table", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.AreEqual("account", command.Table);
		}

		[TestMethod]
		public void Parse_WithTableShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.AreEqual("account", command.Table);
		}

		[TestMethod]
		public void Parse_WithKeyLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.AreEqual("accountnumber=ACC001", command.Key);
		}

		[TestMethod]
		public void Parse_WithKeyShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "-k", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.AreEqual("accountnumber=ACC001", command.Key);
		}

		[TestMethod]
		public void Parse_WithPlainLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.AreEqual("name=Contoso Ltd", command.Plain);
		}

		[TestMethod]
		public void Parse_WithPlainShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "-p", "name=Contoso Ltd");
			Assert.AreEqual("name=Contoso Ltd", command.Plain);
		}

		[TestMethod]
		public void Parse_WithJsonLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--json", "{\"name\":\"Contoso Ltd\"}");
			Assert.AreEqual("{\"name\":\"Contoso Ltd\"}", command.Json);
		}

		[TestMethod]
		public void Parse_WithJsonShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "-j", "{\"name\":\"Contoso Ltd\"}");
			Assert.AreEqual("{\"name\":\"Contoso Ltd\"}", command.Json);
		}

		[TestMethod]
		public void Parse_WithFileLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--file", "C:\\data.json");
			Assert.AreEqual("C:\\data.json", command.File);
		}

		[TestMethod]
		public void Parse_WithFileShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "-f", "C:\\data.json");
			Assert.AreEqual("C:\\data.json", command.File);
		}

		[TestMethod]
		public void Parse_WithReturnLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd", "--return", "name,telephone1");
			Assert.AreEqual("name,telephone1", command.Return);
		}

		[TestMethod]
		public void Parse_WithReturnShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd", "-r", "name");
			Assert.AreEqual("name", command.Return);
		}

		[TestMethod]
		public void Parse_WithDryRunLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd", "--dry-run");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_WithDryRunShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd", "-dr");
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_DryRunShouldBeFalse()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void Parse_Default_ReturnShouldBeNull()
		{
			var command = Utility.TestParseCommand<UpsertCommand>(
				"data", "upsert", "-t", "account", "--key", "accountnumber=ACC001", "--plain", "name=Contoso Ltd");
			Assert.IsNull(command.Return);
		}

		#endregion

		#region Validation Tests

		[TestMethod]
		public void Validate_WhenNeitherPlainNorJsonNorFile_ShouldReturnError()
		{
			var command = new UpsertCommand { Table = "account", Key = "accountnumber=ACC001" };
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Exactly one")));
		}

		[TestMethod]
		public void Validate_WhenTwoPayloadSourcesProvided_ShouldReturnError()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd",
				Json = "{\"name\":\"Contoso Ltd\"}"
			};
			var results = Validate(command);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("Only one")));
		}

		[TestMethod]
		public void Validate_WhenFileDoesNotExist_ShouldReturnError()
		{
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				File = nonExistentFile
			};
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("does not exist")));
		}

		[TestMethod]
		public void Validate_WhenPlainProvided_ShouldPass()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};
			var results = Validate(command);

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_WhenJsonProvided_ShouldPass()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Json = "{\"name\":\"Contoso Ltd\"}"
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
				var command = new UpsertCommand
				{
					Table = "account",
					Key = "accountnumber=ACC001",
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

		[TestMethod]
		public void Validate_WhenKeyIsEmpty_ShouldReturnError()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "   ",
				Plain = "name=Contoso Ltd"
			};
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("--key")));
		}

		#endregion

		#region Helper Methods

		private static List<ValidationResult> Validate(UpsertCommand command)
		{
			var context = new ValidationContext(command);
			return command.Validate(context).ToList();
		}

		#endregion
	}
}
