namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "exportMetadata", HelpText = "Exports the metadata definition of a given column (for documentation purpose)")]
	public class ExportMetadataCommand
	{
		[Option("table", "t", true, "The name of the table containing the column to export")]
		public string TableSchemaName { get; set; } = string.Empty;

		[Option("column", "c", true, "The name of the column to export")]
		public string ColumnSchemaName { get; set; } = string.Empty;

		[Option("output", "o", false, "The name of the folder that will contain the file with the exported metadata. (default: current folder)")]
		public string OutputFilePath { get; set; } = string.Empty;

		[Option("run", "r", false, "Automatically opens the file containing the exported metadata after export.", false)]
		public bool AutoOpenFile { get; set; } = false;
    }
}
