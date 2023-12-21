using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Table
{
    [Command("table", "script", HelpText = "Creates a new script for a table to be copied somewhere else")]
    [Alias("script", "table")]
    public class ScriptCommand
    {
        [Option("schemaName", "sn")]
        public string? SchemaName { get; set; }

        [Option("output", "o", "The name of the folder that will contain the file with the exported metadata. (default: current folder)")]
        public string OutputFilePath { get; set; } = string.Empty;

        [Option("run", "r", "Automatically opens the file containing the exported metadata after export.", false)]
        public bool AutoOpenFile { get; set; } = false;

		[Option("includeTable", "it", "Includes into the resulting file the script command to create table.", false)]
		public bool IncludeTable { get; set; } = false;
	}
}
