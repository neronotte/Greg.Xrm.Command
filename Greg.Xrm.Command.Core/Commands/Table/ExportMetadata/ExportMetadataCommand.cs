using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    [Command("table", "exportMetadata", HelpText = "Exports the metadata definition of a given table (for documentation purpose)")]
    public class ExportMetadataCommand
    {
        [Option("table", "t", Order = 1, HelpText ="The name of the table containing the column to export.")]
        [Required]
        public string TableSchemaName { get; set; } = string.Empty;

        [Option("what", "w", Order = 2, HelpText = "The level of details to export.", DefaultValue = EntityFilters.All)]
        public EntityFilters What { get; set; } = EntityFilters.All;

        [Option("output", "o", Order = 3, HelpText = "The name of the folder that will contain the file with the exported metadata. (default: current folder)")]
        public string OutputFilePath { get; set; } = string.Empty;

        [Option("run", "r", Order = 4, HelpText = "Automatically opens the file containing the exported metadata after export.", DefaultValue = false)]
        public bool AutoOpenFile { get; set; } = false;

        [Option("format", "f", Order = 5, HelpText = "The format of the exported metadata file.", DefaultValue = ExportMetadataFormat.Json)]
        public ExportMetadataFormat Format { get; set; } = ExportMetadataFormat.Json;
    }


    public enum ExportMetadataFormat
    {
        Json,
        Excel
    }
}
