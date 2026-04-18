using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Script
{
	[Command("script", "table", HelpText = "Generates PACX script for a single table.")]
	public class ScriptTableCommand : ICanProvideUsageExample
	{
		[Option("tableName", "tn", Order = 1, HelpText = "Logical name of the table.")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("customPrefixes", "cp", Order = 2, HelpText = "Comma-separated custom prefixes for entities and fields.")]
		[Required]
		public string? CustomPrefixes { get; set; }

		[Option("output", "o", Order = 3, HelpText = "Output directory for generated files.")]
		public string OutputDir { get; set; } = string.Empty;

		[Option("scriptFileName", "script", Order = 4, HelpText = "Name for the generated PACX script file.", DefaultValue = "pacx_datamodel_script.ps1")]
		public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

		[Option("stateFileName", "state", Order = 5, HelpText = "Name of the CSV file that will contain the state fields.", DefaultValue = "state-fields.csv")]
		public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

		[Option("includeStateFields", "i", Order = 6, HelpText = "If true, exports the statecode and statuscode fields as CSV.")]
		public bool WithStateFieldsDefinition { get; set; } = false;



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command generates a PACX script for a single table.");
			writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for the entity, for documentation purposes.");
			writer.WriteParagraph("Example usage:");
			writer.WriteCodeBlock(
				"pacx script table --tableName \"custom_mytable\" --customPrefixes \"custom_\" --output \"C:/output\" --scriptFileName \"myscript.ps1\" --stateFileName \"state-fields.csv\" --includeStateFields",
				"PowerShell");
		}
	}
}
