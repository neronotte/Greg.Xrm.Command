using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Script
{
    [Command("script", "all", HelpText = "Generates PACX scripts for all entities with specified custom prefixes.")]
    public class ScriptAllCommand : ICanProvideUsageExample
    {
        [Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("pacxScriptName", "psn", HelpText = "Name for the generated PACX script file.")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("stateFieldsDefinitionName", "sdn", HelpText = "Name for the State Fields CSV file.")]
        public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

        [Option("withStateFieldsDefinition", "wsd", HelpText = "If true, exports the statecode and statuscode fields as CSV.")]
        public bool WithStateFieldsDefinition { get; set; } = false;

        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("This command generates PACX scripts for all entities with the specified custom prefixes.");
            writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for each entity, for documentation purposes.");
            writer.WriteParagraph("Example usage:");
            writer.WriteCodeBlock("pacx script all --customPrefixs \"custom_\" --output \"C:/output\" --pacxScriptName \"myscript.ps1\" --stateFieldsDefinitionName \"state-fields.csv\" --withStateFieldsDefinition true", "PowerShell");
        }
    }

    [Command("script", "solution", HelpText = "Generates PACX scripts for all tables in a PowerApps solution.")]
    public class ScriptSolutionCommand : ICanProvideUsageExample
    {
        [Option("solutionNames", "sn", HelpText = "Comma-separated list of PowerApps solution names.")]
        [Required]
        public string? SolutionNames { get; set; }

        [Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("pacxScriptName", "psn", HelpText = "Name for the generated PACX script file.")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("stateFieldsDefinitionName", "sdn", HelpText = "Name for the State Fields CSV file.")]
        public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

        [Option("withStateFieldsDefinition", "wsd", HelpText = "If true, exports the statecode and statuscode fields as CSV.")]
        public bool WithStateFieldsDefinition { get; set; } = false;

        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("This command generates PACX scripts for all tables in one or more PowerApps solutions.");
            writer.WriteParagraph("If requested, the generated CSV file will contain only the statecode and statuscode fields for each entity, for documentation purposes.");
            writer.WriteParagraph("Example usage:");
            writer.WriteCodeBlock("pacx script solution --solutionNames \"Solution1,Solution2\" --customPrefixs \"custom_\" --output \"C:/output\" --pacxScriptName \"myscript.ps1\" --stateFieldsDefinitionName \"state-fields.csv\" --withStateFieldsDefinition true", "PowerShell");
        }
    }

    [Command("script", "table", HelpText = "Generates PACX script for a single table.")]
    public class ScriptTableCommand : ICanProvideUsageExample
    {
        [Option("tableName", "tn", HelpText = "Logical name of the table.")]
        [Required]
        public string? TableName { get; set; }

        [Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("pacxScriptName", "psn", HelpText = "Name for the generated PACX script file.")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("stateFieldsDefinitionName", "sdn", HelpText = "Name for the State Fields CSV file.")]
        public string StateFieldsDefinitionName { get; set; } = "state-fields.csv";

        [Option("withStateFieldsDefinition", "wsd", HelpText = "If true, exports the statecode and statuscode fields as CSV.")]
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
