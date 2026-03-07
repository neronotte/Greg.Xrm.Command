using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "exportMetadata", HelpText = "Exports the metadata definition of a given column (for documentation purpose)")]
	public class ExportMetadataCommand
	{
		[Option("table", "t", Order = 1, HelpText = "The name of the table containing the column to export")]
		[Required]
		public string TableSchemaName { get; set; } = string.Empty;

		[Option("column", "c", Order = 2, HelpText ="The name of the column to export")]
		[Required]
		public string ColumnSchemaName { get; set; } = string.Empty;

		[Option("output", "o", Order = 3, HelpText = "The name of the folder that will contain the file with the exported metadata. (default: current folder)")]
		public string OutputFilePath { get; set; } = string.Empty;

		[Option("run", "r", Order = 4, HelpText = "Automatically opens the file containing the exported metadata after export.", DefaultValue = false)]
		public bool AutoOpenFile { get; set; } = false;
    }
}
