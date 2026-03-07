using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Script
{
	[Command("script", "solution", HelpText = "Generates PACX scripts for all tables in a PowerApps solution.")]
    public class ScriptSolutionCommand : ICanProvideUsageExample
    {
        [Option("solutionNames", "sn", Order = 1, HelpText = "Comma-separated list of PowerApps solution names.")]
        [Required]
        public string? SolutionNames { get; set; }

        [Option("customPrefixes", "cp", Order = 2, HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", Order = 3, HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("scriptFileName", "script", Order = 4, HelpText = "Name for the generated PACX script file.", DefaultValue = "pacx_datamodel_script.ps1")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("stateFileName", "state", Order = 5, HelpText = "Name of the CSV file that will contain the state fields.", DefaultValue = "state-fields.csv")]
        public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

        [Option("includeStateFields", "i", Order = 6, HelpText = "If true, exports the statecode and statuscode fields as CSV.", DefaultValue = false)]
        public bool WithStateFieldsDefinition { get; set; } = false;

        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("This command generates PACX scripts for all tables in one or more PowerApps solutions.");
            writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for each entity, for documentation purposes.");
            writer.WriteParagraph("Example usage:");
            writer.WriteCodeBlock(
                "pacx script solution --solutionNames \"Solution1,Solution2\" --customPrefixs \"custom_\" --output \"C:/output\" --scriptFileName \"myscript.ps1\" --stateFileName \"state-fields.csv\" --includeStateFields",
                "PowerShell");
        }
    }
}
