using Greg.Xrm.Command.Commands.Script.MetadataExtractor;
using Greg.Xrm.Command.Commands.Script.Models;
using Greg.Xrm.Command.Commands.Script.Service;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Script
{
	[TestClass]
	public class ScriptTableCommandExecutorTests
	{
		/// <summary>
		/// Unit test with mocked dependencies
		/// </summary>
		[TestMethod]
		public async Task ExecuteAsync_WithValidTable_ShouldExtractMetadata()
		{
			// Arrange
			var tableName = "incident";
			var customPrefixes = "ava_";
			var outputDir = "C:/output";
			var scriptFileName = "incident_datamodel.ps1";
			var stateFileName = "incident_state-fields.csv";

			var mockOutput = new Mock<IOutput>();
			var mockMetadataExtractor = new Mock<IScriptMetadataExtractor>();
			var mockServiceRepository = new Mock<IOrganizationServiceRepository>();
			var mockOrganizationService = new Mock<IOrganizationServiceAsync2>();

			// Setup mock entity metadata
			var mockEntity = new Extractor_EntityMetadata
			{
				SchemaName = tableName
            };

			mockMetadataExtractor
				.Setup(x => x.GetTableAsync(tableName, It.IsAny<List<string>>()))
				.ReturnsAsync(mockEntity);

			mockServiceRepository
				.Setup(x => x.GetCurrentConnectionAsync())
				.ReturnsAsync(mockOrganizationService.Object);

			var extractionService = new ScriptExtractionService(
				mockOutput.Object,
				mockMetadataExtractor.Object);

			var executor = new ScriptTableCommandExecutor(extractionService);

			var command = new ScriptTableCommand
			{
				TableName = tableName,
				CustomPrefixes = customPrefixes,
				OutputDir = outputDir,
				PacxScriptName = scriptFileName,
				StateFieldsDefinitionName = stateFileName,
				WithStateFieldsDefinition = true
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			mockMetadataExtractor.Verify(
				x => x.GetTableAsync(tableName, It.IsAny<List<string>>()),
				Times.Once);
		}

		/// <summary>
		/// Unit test for table not found scenario
		/// </summary>
		[TestMethod]
		public async Task ExecuteAsync_WithInvalidTable_ShouldFail()
		{
			// Arrange
			var tableName = "nonexistent_table";
			
			var mockOutput = new Mock<IOutput>();
			var mockMetadataExtractor = new Mock<IScriptMetadataExtractor>();

			mockMetadataExtractor
				.Setup(x => x.GetTableAsync(tableName, It.IsAny<List<string>>()))
				.ReturnsAsync((Extractor_EntityMetadata?)null);

			var extractionService = new ScriptExtractionService(
				mockOutput.Object,
				mockMetadataExtractor.Object);

			var executor = new ScriptTableCommandExecutor(extractionService);

			var command = new ScriptTableCommand
			{
				TableName = tableName,
				CustomPrefixes = "ava_",
				OutputDir = "C:/output"
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsFalse(result.IsSuccess);
			mockOutput.Verify(
				x => x.WriteLine(
					It.Is<string>(s => s.Contains("Table not found")),
					It.IsAny<ConsoleColor>()),
				Times.Once);
		}

		/// <summary>
		/// Integration test - requires real Dataverse connection
		/// Set [Ignore] attribute to skip during normal test runs
		/// Remove [Ignore] when you want to debug locally
		/// </summary>
		[TestMethod]
		public async Task ExecuteAsync_Integration_WithRealConnection()
		{
			// Arrange
			var tableName = "";
			var customPrefixes = "";
			var outputDir = TestConfiguration.GetTestOutputDirectory();
			var scriptFileName = "";
			var stateFileName = "";
			
			// Alternative: Use explicit parameters
			var serviceClient = TestConfiguration.CreateServiceClient(
				url: "",
				clientId: "",
				clientSecret: ""
            );

			Assert.IsTrue(serviceClient.IsReady, $"Failed to connect to Dataverse: {serviceClient.LastError}");

			var output = new OutputToMemory(); // Or use OutputToConsole for debugging
			var mockServiceRepository = new Mock<IOrganizationServiceRepository>();
			mockServiceRepository
				.Setup(x => x.GetCurrentConnectionAsync())
				.ReturnsAsync(serviceClient);

			var scriptBuilder = new ScriptBuilder();
			var metadataExtractor = new ScriptMetadataExtractor(
				mockServiceRepository.Object,
				scriptBuilder);

			var extractionService = new ScriptExtractionService(
				output,
				metadataExtractor);

			var executor = new ScriptTableCommandExecutor(extractionService);

			var command = new ScriptTableCommand
			{
				TableName = tableName,
				CustomPrefixes = customPrefixes,
				OutputDir = outputDir,
				PacxScriptName = scriptFileName,
				StateFieldsDefinitionName = stateFileName,
				WithStateFieldsDefinition = true
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			Assert.IsTrue(result.IsSuccess, $"Command failed: {result.ErrorMessage}");
			
			// Verify output files were created
			var scriptFilePath = Path.Combine(outputDir, scriptFileName);
			Assert.IsTrue(File.Exists(scriptFilePath), $"Script file not created at {scriptFilePath}");

			var stateFilePath = Path.Combine(outputDir, stateFileName);
			Assert.IsTrue(File.Exists(stateFilePath), $"State file not created at {stateFilePath}");

			// Clean up
			serviceClient.Dispose();
		}
	}
}