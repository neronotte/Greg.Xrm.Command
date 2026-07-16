using Greg.Xrm.Command.Commands.Data.Query;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Moq;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data
{
	[TestClass]
	public class QueryCommandExecutorTest
	{
		private Mock<IOrganizationServiceRepository> organizationServiceRepositoryMock = null!;
		private Mock<IOrganizationServiceAsync2> organizationServiceMock = null!;
		private Mock<IQueryExecutorFactory> queryExecutorFactoryMock = null!;
		private Mock<IQueryOutputFormatterFactory> queryOutputFormatterFactoryMock = null!;
		private Mock<IQueryExecutor> queryExecutorMock = null!;
		private Mock<IQueryOutputFormatter> queryOutputFormatterMock = null!;
		private OutputToMemory output = null!;
		private Mock<IAnsiConsole> consoleMock = null!;
		private QueryCommandExecutor executor = null!;

		[TestInitialize]
		public void Setup()
		{
			output = new OutputToMemory();
			consoleMock = new Mock<IAnsiConsole>();

			organizationServiceMock = new Mock<IOrganizationServiceAsync2>();
			organizationServiceRepositoryMock = new Mock<IOrganizationServiceRepository>();
			organizationServiceRepositoryMock
				.Setup(m => m.GetCurrentConnectionAsync())
				.ReturnsAsync(organizationServiceMock.Object);

			queryExecutorMock = new Mock<IQueryExecutor>();
			queryExecutorFactoryMock = new Mock<IQueryExecutorFactory>();

			queryOutputFormatterMock = new Mock<IQueryOutputFormatter>();
			queryOutputFormatterFactoryMock = new Mock<IQueryOutputFormatterFactory>();

			executor = new QueryCommandExecutor(
				output,
				consoleMock.Object,
				organizationServiceRepositoryMock.Object,
				queryExecutorFactoryMock.Object,
				queryOutputFormatterFactoryMock.Object);
		}

		#region Query Text Source Tests

		[TestMethod]
		public async Task ExecuteAsync_WithDirectQuery_ShouldUseQueryText()
		{
			// Arrange
			var queryText = "<fetch><entity name='account'/></fetch>";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			SetupMocksForSuccessfulExecution(queryText);

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryExecutorFactoryMock.Verify(x => x.DetectExecutorFromQueryText(queryText), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithQueryFile_ShouldReadFromFile()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			var queryText = "SELECT name FROM account";
			try
			{
				await File.WriteAllTextAsync(tempFile, queryText);

				var command = new QueryCommand
				{
					Query = null,
					QueryFile = tempFile,
					OutputFormat = QueryCommand.OutputFormats.JSON
				};

				SetupMocksForSuccessfulExecution(queryText);

				// Act
				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess);
				queryExecutorFactoryMock.Verify(x => x.DetectExecutorFromQueryText(queryText), Times.Once);
				Assert.IsTrue(output.ToString().Contains("Loading query text from file"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_WithQueryFileContainingEmptyText_ShouldFail()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(tempFile, "   ");

				var command = new QueryCommand
				{
					Query = null,
					QueryFile = tempFile,
					OutputFormat = QueryCommand.OutputFormats.JSON
				};

				// Act
				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				// Assert
				Assert.IsFalse(result.IsSuccess);
				Assert.IsTrue(result.ErrorMessage!.Contains("Query text is empty"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_WithNonExistentQueryFile_ShouldFail()
		{
			// Arrange
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
			var command = new QueryCommand
			{
				Query = null,
				QueryFile = nonExistentFile,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error reading query file"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryIsWhitespaceAndNoQueryFile_ShouldFail()
		{
			// Arrange
			// When Query is whitespace, the executor tries to read from QueryFile
			// Since QueryFile is null, this will fail with "Error reading query file"
			var command = new QueryCommand
			{
				Query = "   ",
				QueryFile = null,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error reading query file"));
		}

		#endregion

		#region Query Executor Factory Tests

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryExecutorFactoryThrowsNotSupported_ShouldFail()
		{
			// Arrange
			var queryText = "INVALID QUERY";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Throws(new NotSupportedException("Unsupported query format"));

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Unsupported query format"));
		}

		#endregion

		#region Output Formatter Factory Tests

		[TestMethod]
		public async Task ExecuteAsync_WhenOutputFormatterFactoryThrowsNotSupported_ShouldFail()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.json"
			};

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Returns(queryExecutorMock.Object);

			queryOutputFormatterFactoryMock
				.Setup(x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, "output.json"))
				.Throws(new NotSupportedException("Unsupported output format"));

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Unsupported output format"));
		}

		#endregion

		#region Query Execution Tests

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryExecutionSucceeds_ShouldReturnSuccess()
		{
			// Arrange
			var queryText = "<fetch><entity name='account'/></fetch>";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			var entities = new List<Entity>
			{
				new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" }
			};

			SetupMocksForSuccessfulExecution(queryText, entities);

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryExecutorMock.Verify(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryExecutionFails_ShouldReturnFailure()
		{
			// Arrange
			var queryText = "<fetch><entity name='account'/></fetch>";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Returns(queryExecutorMock.Object);

			queryOutputFormatterFactoryMock
				.Setup(x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, null))
				.Returns(queryOutputFormatterMock.Object);

			queryExecutorMock
				.Setup(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Query execution failed"));

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error executing query"));
			Assert.IsTrue(result.ErrorMessage!.Contains("Query execution failed"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithEmptyResults_ShouldStillSucceed()
		{
			// Arrange
			var queryText = "SELECT * FROM account WHERE name = 'NonExistent'";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			SetupMocksForSuccessfulExecution(queryText, new List<Entity>());

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
		}

		#endregion

		#region Output Formatting Tests

		[TestMethod]
		public async Task ExecuteAsync_WithJsonFormat_ShouldUseJsonFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.json"
			};

			SetupMocksForSuccessfulExecution(queryText, outputFormat: QueryCommand.OutputFormats.JSON, outputFileName: "output.json");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterFactoryMock.Verify(
				x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, "output.json"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCsvFormat_ShouldUseCsvFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.CSV,
				OutputFileName = "output.csv"
			};

			SetupMocksForSuccessfulExecution(queryText, outputFormat: QueryCommand.OutputFormats.CSV, outputFileName: "output.csv");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterFactoryMock.Verify(
				x => x.BuildFormatter(QueryCommand.OutputFormats.CSV, "output.csv"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithXmlFormat_ShouldUseXmlFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.XML,
				OutputFileName = "output.xml"
			};

			SetupMocksForSuccessfulExecution(queryText, outputFormat: QueryCommand.OutputFormats.XML, outputFileName: "output.xml");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterFactoryMock.Verify(
				x => x.BuildFormatter(QueryCommand.OutputFormats.XML, "output.xml"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithExcelFormat_ShouldUseExcelFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.Excel,
				OutputFileName = "output.xlsx"
			};

			SetupMocksForSuccessfulExecution(queryText, outputFormat: QueryCommand.OutputFormats.Excel, outputFileName: "output.xlsx");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterFactoryMock.Verify(
				x => x.BuildFormatter(QueryCommand.OutputFormats.Excel, "output.xlsx"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithAutoRun_ShouldPassAutoRunToFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.json",
				OutputFileAutoRun = true
			};

			var entities = new List<Entity>();
			SetupMocksForSuccessfulExecution(queryText, entities, QueryCommand.OutputFormats.JSON, "output.json");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterMock.Verify(
				x => x.Print(It.IsAny<IReadOnlyCollection<Entity>>(), true, It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithoutAutoRun_ShouldPassFalseToFormatter()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = "output.json",
				OutputFileAutoRun = false
			};

			var entities = new List<Entity>();
			SetupMocksForSuccessfulExecution(queryText, entities, QueryCommand.OutputFormats.JSON, "output.json");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterMock.Verify(
				x => x.Print(It.IsAny<IReadOnlyCollection<Entity>>(), false, It.IsAny<CancellationToken>()),
				Times.Once);
		}

		#endregion

		#region Connection Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldConnectToDataverse()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			SetupMocksForSuccessfulExecution(queryText);

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			organizationServiceRepositoryMock.Verify(x => x.GetCurrentConnectionAsync(), Times.Once);
			Assert.IsTrue(output.ToString().Contains("Connecting to the current dataverse environment"));
		}

		#endregion

		#region Cancellation Tests

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryExecutionThrowsOperationCanceled_ShouldReturnError()
		{
			// Arrange
			// Note: The executor catches exceptions from query execution and returns a failure result
			// rather than letting the OperationCanceledException propagate
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Returns(queryExecutorMock.Object);

			queryOutputFormatterFactoryMock
				.Setup(x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, null))
				.Returns(queryOutputFormatterMock.Object);

			queryExecutorMock
				.Setup(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException("Operation was cancelled"));

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error executing query"));
		}

		#endregion

		#region Output Messages Tests

		[TestMethod]
		public async Task ExecuteAsync_OnSuccess_ShouldOutputProgressMessages()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			SetupMocksForSuccessfulExecution(queryText);

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			var outputText = output.ToString();
			Assert.IsTrue(outputText.Contains("Connecting to the current dataverse environment"));
			Assert.IsTrue(outputText.Contains("Executing query"));
			Assert.IsTrue(outputText.Contains("Done"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryFileFails_ShouldOutputFailedMessage()
		{
			// Arrange
			var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
			var command = new QueryCommand
			{
				Query = null,
				QueryFile = nonExistentFile,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			var outputText = output.ToString();
			Assert.IsTrue(outputText.Contains("Loading query text from file"));
			Assert.IsTrue(outputText.Contains("FAILED"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenQueryExecutionFails_ShouldOutputFailedMessage()
		{
			// Arrange
			var queryText = "SELECT * FROM account";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON
			};

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Returns(queryExecutorMock.Object);

			queryOutputFormatterFactoryMock
				.Setup(x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, null))
				.Returns(queryOutputFormatterMock.Object);

			queryExecutorMock
				.Setup(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Query failed"));

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			var outputText = output.ToString();
			Assert.IsTrue(outputText.Contains("Executing query"));
			Assert.IsTrue(outputText.Contains("FAILED"));
		}

		#endregion

		#region Integration-like Tests

		[TestMethod]
		public async Task ExecuteAsync_FullSuccessfulFlow_FetchXmlQuery()
		{
			// Arrange
			var queryText = "<fetch><entity name='account'><attribute name='name'/></entity></fetch>";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.JSON,
				OutputFileName = null,
				OutputFileAutoRun = false
			};

			var entities = new List<Entity>
			{
				new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
				new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2" },
				new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 3" }
			};

			SetupMocksForSuccessfulExecution(queryText, entities);

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryExecutorFactoryMock.Verify(x => x.DetectExecutorFromQueryText(queryText), Times.Once);
			queryOutputFormatterFactoryMock.Verify(x => x.BuildFormatter(QueryCommand.OutputFormats.JSON, null), Times.Once);
			organizationServiceRepositoryMock.Verify(x => x.GetCurrentConnectionAsync(), Times.Once);
			queryExecutorMock.Verify(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()), Times.Once);
			queryOutputFormatterMock.Verify(x => x.Print(It.Is<IReadOnlyCollection<Entity>>(e => e.Count == 3), false, It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_FullSuccessfulFlow_SqlQuery()
		{
			// Arrange
			var queryText = "SELECT name, accountid FROM account WHERE statecode = 0";
			var command = new QueryCommand
			{
				Query = queryText,
				OutputFormat = QueryCommand.OutputFormats.CSV,
				OutputFileName = "results.csv",
				OutputFileAutoRun = true
			};

			var entities = new List<Entity>
			{
				new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Corp" }
			};

			SetupMocksForSuccessfulExecution(queryText, entities, QueryCommand.OutputFormats.CSV, "results.csv");

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess);
			queryOutputFormatterMock.Verify(
				x => x.Print(It.IsAny<IReadOnlyCollection<Entity>>(), true, It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_FullFlow_WithQueryFile()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			var queryText = "SELECT fullname, emailaddress1 FROM contact";
			try
			{
				await File.WriteAllTextAsync(tempFile, queryText);

				var command = new QueryCommand
				{
					Query = null,
					QueryFile = tempFile,
					OutputFormat = QueryCommand.OutputFormats.Excel,
					OutputFileName = "contacts.xlsx",
					OutputFileAutoRun = false
				};

				var entities = new List<Entity>
				{
					new Entity("contact") { Id = Guid.NewGuid(), ["fullname"] = "John Doe", ["emailaddress1"] = "john@example.com" }
				};

				SetupMocksForSuccessfulExecution(queryText, entities, QueryCommand.OutputFormats.Excel, "contacts.xlsx");

				// Act
				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				// Assert
				Assert.IsTrue(result.IsSuccess);
				var outputText = output.ToString();
				Assert.IsTrue(outputText.Contains("Loading query text from file"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		#endregion

		#region Helper Methods

		private void SetupMocksForSuccessfulExecution(
			string queryText,
			List<Entity>? entities = null,
			QueryCommand.OutputFormats outputFormat = QueryCommand.OutputFormats.JSON,
			string? outputFileName = null)
		{
			entities ??= new List<Entity>();

			queryExecutorFactoryMock
				.Setup(x => x.DetectExecutorFromQueryText(queryText))
				.Returns(queryExecutorMock.Object);

			queryOutputFormatterFactoryMock
				.Setup(x => x.BuildFormatter(outputFormat, outputFileName))
				.Returns(queryOutputFormatterMock.Object);

			queryExecutorMock
				.Setup(x => x.ExecuteQueryAsync(organizationServiceMock.Object, It.IsAny<CancellationToken>()))
				.ReturnsAsync(entities);

			queryOutputFormatterMock
				.Setup(x => x.Print(It.IsAny<IReadOnlyCollection<Entity>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
		}

		#endregion
	}
}
