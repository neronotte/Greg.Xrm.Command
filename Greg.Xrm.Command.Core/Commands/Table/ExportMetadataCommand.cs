using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "exportMetadata", HelpText = "Exports the metadata definition of a given table (for documentation purpose)")]
	public class ExportMetadataCommand
	{
		[Option("table", "t", "The name of the table containing the column to export.")]
		[Required]
		public string TableSchemaName { get; set; } = string.Empty;

		[Option("what", "w", "The level of details to export.", DefaultValue = EntityFilters.All)]
		public EntityFilters What { get; set; } = EntityFilters.All;

		[Option("output", "o", "The name of the folder that will contain the file with the exported metadata. (default: current folder)")]
		public string OutputFilePath { get; set; } = string.Empty;

		[Option("run", "r", "Automatically opens the file containing the exported metadata after export.", false)]
		public bool AutoOpenFile { get; set; } = false;
    }
}
