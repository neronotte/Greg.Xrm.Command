using Greg.Xrm.Command.Commands.Script.MetadataExtractor;
using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
	[TestClass]
	public class ScriptTableCommandParameterizedTests
	{
		[TestMethod]
		[DataRow("incident", "ava_", "incident_datamodel.ps1", true)]
		[DataRow("account", "custom_,new_", "account_script.ps1", false)]
		[DataRow("contact", "", "contact_script.ps1", false)]
		public async Task ExecuteAsync_WithVariousParameters_ShouldParseCorrectly(
			string tableName,
			string customPrefixes,
			string scriptFileName,
			bool includeStateFields)
		{
			// Arrange
			var mockOutput = new Mock<IOutput>();
			var mockMetadataExtractor = new Mock<IScriptMetadataExtractor>();

			var mockEntity = new Models.Extractor_EntityMetadata
			{
				SchemaName = tableName
			};

			mockMetadataExtractor
				.Setup(x => x.GetTableAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
				.ReturnsAsync(mockEntity);

			var extractionService = new ScriptExtractionService(
				mockOutput.Object,
				mockMetadataExtractor.Object);

			var executor = new ScriptTableCommandExecutor(extractionService);

			var command = new ScriptTableCommand
			{
				TableName = tableName,
				CustomPrefixes = customPrefixes,
				OutputDir = TestConfiguration.GetTestOutputDirectory(),
				PacxScriptName = scriptFileName,
				WithStateFieldsDefinition = includeStateFields
			};

			// Act
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			// Assert
			mockMetadataExtractor.Verify(
				x => x.GetTableAsync(
					tableName,
					It.Is<List<string>>(prefixes => 
						string.IsNullOrEmpty(customPrefixes) ? prefixes.Count == 0 : prefixes.Count > 0)),
				Times.Once);
		}

		[TestMethod]
		public void ScriptTableCommand_Parsing_ShouldWorkCorrectly()
		{
			// Test command parsing
			var command = Utility.TestParseCommand<ScriptTableCommand>(
				"script", "table",
				"--tableName", "incident",
				"--customPrefixs", "ava_",
				"--output", "C:/output",
				"--scriptFileName", "incident_datamodel.ps1",
				"--stateFileName", "incident_state-fields.csv",
				"--includeStateFields"
			);

			Assert.AreEqual("incident", command.TableName);
			Assert.AreEqual("ava_", command.CustomPrefixes);
			Assert.AreEqual("C:/output", command.OutputDir);
			Assert.AreEqual("incident_datamodel.ps1", command.PacxScriptName);
			Assert.AreEqual("incident_state-fields.csv", command.StateFieldsDefinitionName);
			Assert.IsTrue(command.WithStateFieldsDefinition);
		}
	}
}