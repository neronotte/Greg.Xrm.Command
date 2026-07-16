using Greg.Xrm.Command.Commands.Data;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Data
{
	[TestClass]
	public class QueryCommandTest
	{
		#region Parsing Tests

		[TestMethod]
		public void Parse_WithQueryLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "--query", "<fetch><entity name='account'/></fetch>");
			Assert.AreEqual("<fetch><entity name='account'/></fetch>", command.Query);
		}

		[TestMethod]
		public void Parse_WithQueryShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "<fetch><entity name='account'/></fetch>");
			Assert.AreEqual("<fetch><entity name='account'/></fetch>", command.Query);
		}

		[TestMethod]
		public void Parse_WithQueryFileLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "--query-file", "C:\\temp\\query.xml");
			Assert.AreEqual("C:\\temp\\query.xml", command.QueryFile);
		}

		[TestMethod]
		public void Parse_WithQueryFileShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-qf", "C:\\temp\\query.xml");
			Assert.AreEqual("C:\\temp\\query.xml", command.QueryFile);
		}

		[TestMethod]
		public void Parse_WithFormatLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "--format", "CSV");
			Assert.AreEqual(QueryCommand.OutputFormats.CSV, command.OutputFormat);
		}

		[TestMethod]
		public void Parse_WithFormatShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "-f", "XML");
			Assert.AreEqual(QueryCommand.OutputFormats.XML, command.OutputFormat);
		}

		[TestMethod]
		public void Parse_WithOutputLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "--output", "C:\\temp\\output.json");
			Assert.AreEqual("C:\\temp\\output.json", command.OutputFileName);
		}

		[TestMethod]
		public void Parse_WithOutputShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "-o", "C:\\temp\\output.json");
			Assert.AreEqual("C:\\temp\\output.json", command.OutputFileName);
		}

		[TestMethod]
		public void Parse_WithAutoRunLongOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "-o", "C:\\temp\\output.json", "--auto-run");
			Assert.IsTrue(command.OutputFileAutoRun);
		}

		[TestMethod]
		public void Parse_WithAutoRunShortOption_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account", "-o", "C:\\temp\\output.json", "-run");
			Assert.IsTrue(command.OutputFileAutoRun);
		}

		[TestMethod]
		public void Parse_DefaultFormat_ShouldBeJson()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account");
			Assert.AreEqual(QueryCommand.OutputFormats.JSON, command.OutputFormat);
		}

		[TestMethod]
		public void Parse_DefaultAutoRun_ShouldBeFalse()
		{
			var command = Utility.TestParseCommand<QueryCommand>("data", "query", "-q", "SELECT * FROM account");
			Assert.IsFalse(command.OutputFileAutoRun);
		}

		[TestMethod]
		public void Parse_WithAllOptions_ShouldWork()
		{
			var command = Utility.TestParseCommand<QueryCommand>(
				"data", "query",
				"-q", "SELECT * FROM account",
				"-f", "Excel",
				"-o", "C:\\temp\\output.xlsx",
				"--auto-run");

			Assert.AreEqual("SELECT * FROM account", command.Query);
			Assert.AreEqual(QueryCommand.OutputFormats.Excel, command.OutputFormat);
			Assert.AreEqual("C:\\temp\\output.xlsx", command.OutputFileName);
			Assert.IsTrue(command.OutputFileAutoRun);
		}

		#endregion

		#region Validation Tests - Query/QueryFile Mutual Exclusivity

		[TestMethod]
		public void Validate_WhenBothQueryAndQueryFileAreEmpty_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = null,
				QueryFile = null
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("Either Query or QueryFile must be provided"));
		}

		[TestMethod]
		public void Validate_WhenBothQueryAndQueryFileAreWhitespace_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "   ",
				QueryFile = "   "
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("Either Query or QueryFile must be provided"));
		}

		[TestMethod]
		public void Validate_WhenBothQueryAndQueryFileAreProvided_ShouldReturnError()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				var command = new QueryCommand
				{
					Query = "SELECT * FROM account",
					QueryFile = tempFile
				};

				var validationResults = ValidateCommand(command);

				Assert.AreEqual(1, validationResults.Count);
				Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("Only one of Query or QueryFile can be provided"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void Validate_WhenOnlyQueryIsProvided_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				QueryFile = null,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenOnlyQueryFileIsProvidedAndExists_ShouldPass()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				var command = new QueryCommand
				{
					Query = null,
					QueryFile = tempFile,
					OutputFormat = QueryCommand.OutputFormats.JSON
				};

				var validationResults = ValidateCommand(command);

				Assert.AreEqual(0, validationResults.Count);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void Validate_WhenQueryFileDoesNotExist_ShouldReturnError()
		{
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");

			var command = new QueryCommand
			{
				Query = null,
				QueryFile = nonExistentFile
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("does not exist"));
		}

		#endregion

		#region Validation Tests - Excel Format Requires Output File

		[TestMethod]
		public void Validate_WhenFormatIsExcelAndNoOutputFile_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = null
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("output file name must be provided when the format is Excel"));
		}

		[TestMethod]
		public void Validate_WhenFormatIsExcelAndOutputFileIsWhitespace_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = "   "
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("output file name must be provided when the format is Excel"));
		}

		[TestMethod]
		public void Validate_WhenFormatIsExcelWithCorrectExtension_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = "output.xlsx"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		#endregion

		#region Validation Tests - File Extension Matching

		[TestMethod]
		public void Validate_WhenFormatIsJsonWithJsonExtension_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.json"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenFormatIsJsonWithWrongExtension_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.xml"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("extension does not match"));
		}

		[TestMethod]
		public void Validate_WhenFormatIsCsvWithCsvExtension_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.CSV,
				OutputFileName = "output.csv"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenFormatIsCsvWithWrongExtension_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.CSV,
				OutputFileName = "output.json"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("extension does not match"));
		}

		[TestMethod]
		public void Validate_WhenFormatIsXmlWithXmlExtension_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.XML,
				OutputFileName = "output.xml"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenFormatIsXmlWithWrongExtension_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.XML,
				OutputFileName = "output.csv"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("extension does not match"));
		}

		[TestMethod]
		public void Validate_WhenFormatIsExcelWithXlsxExtension_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = "output.xlsx"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenFormatIsExcelWithWrongExtension_ShouldReturnError()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = "output.xls"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(1, validationResults.Count);
			Assert.IsTrue(validationResults[0].ErrorMessage!.Contains("extension does not match"));
		}

		[TestMethod]
		public void Validate_ExtensionCheck_ShouldBeCaseInsensitive()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.JSON"
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		#endregion

		#region Validation Tests - No Output File (Console Output)

		[TestMethod]
		public void Validate_WhenNoOutputFileAndFormatIsJson_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = null
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenNoOutputFileAndFormatIsCsv_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.CSV,
				OutputFileName = null
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		[TestMethod]
		public void Validate_WhenNoOutputFileAndFormatIsXml_ShouldPass()
		{
			var command = new QueryCommand
			{
				Query = "SELECT * FROM account",
				OutputFormat = QueryCommand.OutputFormats.XML,
				OutputFileName = null
			};

			var validationResults = ValidateCommand(command);

			Assert.AreEqual(0, validationResults.Count);
		}

		#endregion

		#region Helper Methods

		private static List<ValidationResult> ValidateCommand(QueryCommand command)
		{
			var validationContext = new ValidationContext(command);
			return command.Validate(validationContext).ToList();
		}

		#endregion
	}
}
