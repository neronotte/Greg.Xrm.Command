using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Script
{
	[Command("script", "table", HelpText = "Generates PACX script for a single table.")]
    public class ScriptTableCommand : ICanProvideUsageExample
    {
        [Option("tableName", "tn", HelpText = "Logical name of the table.")]
        [Required]
        public string TableName { get; set; } = string.Empty;

		[Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

		[Option("scriptFileName", "script", HelpText = "Name for the generated PACX script file.", DefaultValue = "pacx_datamodel_script.ps1")]
		public string? PacxScriptName { get; set; }

		[Option("stateFileName", "state", HelpText = "Name of the CSV file that will contain the state fields.", DefaultValue = "state-fields.csv")]
		public string? StateFieldsDefinitionName { get; set; }

		[Option("includeStateFields", "i", HelpText = "If true, exports the statecode and statuscode fields as CSV.")]
		public bool WithStateFieldsDefinition { get; set; } = false;



		public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("This command generates a PACX script for a single table.");
            writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for the entity, for documentation purposes.");
            writer.WriteParagraph("Example usage:");
            writer.WriteCodeBlock("pacx script table --tableName \"account\" --customPrefixs \"custom_\" --output \"C:/output\" --pacxScriptName \"myscript.ps1\" --stateFieldsDefinitionName \"state-fields.csv\" --withStateFieldsDefinition true", "PowerShell");
        }
    }
}
