using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Script
{
    [Command("script", "all", HelpText = "Generates PACX scripts for all entities with specified custom prefixes.")]
    public class ScriptAllCommand
    {
        [Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("pacxScriptName", "psn", HelpText = "Name for the generated PACX script file.")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("optionsetDefinitionName", "odn", HelpText = "Name for the OptionSet CSV file.")]
        public string OptionSetDefinitionName { get; set; } = "optionset-definitions.csv";

        [Option("withOptionsetDefinition", "wod", HelpText = "If true, exports the OptionSet definitions as CSV.")]
        public bool WithOptionsetDefinition { get; set; } = false;
    }

    [Command("script", "solution", HelpText = "Generates PACX scripts for all tables in a PowerApps solution.")]
    public class ScriptSolutionCommand
    {
        [Option("solutionName", "sn", HelpText = "Name of the PowerApps solution.")]
        [Required]
        public string? SolutionName { get; set; }

        [Option("customPrefixs", "cp", HelpText = "Comma-separated custom prefixes for entities and fields.")]
        [Required]
        public string? CustomPrefixs { get; set; }

        [Option("output", "o", HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = string.Empty;

        [Option("pacxScriptName", "psn", HelpText = "Name for the generated PACX script file.")]
        public string PacxScriptName { get; set; } = "pacx_datamodel_script.ps1";

        [Option("optionsetDefinitionName", "odn", HelpText = "Name for the OptionSet CSV file.")]
        public string OptionSetDefinitionName { get; set; } = "optionset-definitions.csv";

        [Option("withOptionsetDefinition", "wod", HelpText = "If true, exports the OptionSet definitions as CSV.")]
        public bool WithOptionsetDefinition { get; set; } = false;
    }

    [Command("script", "table", HelpText = "Generates PACX script for a single table.")]
    public class ScriptTableCommand
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

        [Option("optionsetDefinitionName", "odn", HelpText = "Name for the OptionSet CSV file.")]
        public string OptionSetDefinitionName { get; set; } = "optionset-definitions.csv";

        [Option("withOptionsetDefinition", "wod", HelpText = "If true, exports the OptionSet definitions as CSV.")]
        public bool WithOptionsetDefinition { get; set; } = false;
    }
}
