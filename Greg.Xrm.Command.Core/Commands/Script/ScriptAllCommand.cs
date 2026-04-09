using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Script
{
    [Command("script", "all", HelpText = "Generates PACX scripts for all entities with specified custom prefixes.")]
    public class ScriptAllCommand : ICanProvideUsageExample
    {
        [Option("customPrefixes", "cp", Order = 1, HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixes { get; set; }

        [Option("output", "o", Order = 2, HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("scriptFileName", "script", Order = 3, HelpText = "Name for the generated PACX script file.", DefaultValue = "pacx_datamodel_script.ps1")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("stateFileName", "state", Order = 4, HelpText = "Name of the CSV file that will contain the state fields.", DefaultValue = "state-fields.csv")]
        public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

        [Option("includeStateFields", "i", Order = 5, HelpText = "If true, exports the statecode and statuscode fields as CSV.", DefaultValue = false)]
        public bool WithStateFieldsDefinition { get; set; }

        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("This command generates PACX scripts for all entities with the specified custom prefixes.");
            writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for each entity, for documentation purposes.");
            writer.WriteParagraph("Example usage:");
            writer.WriteCodeBlock(
                "pacx script all --customPrefixes \"custom_\" --output \"C:/output\" --scriptFileName \"myscript.ps1\" --stateFileName \"state-fields.csv\" --includeStateFields",
                "PowerShell");
        }
    }
}
