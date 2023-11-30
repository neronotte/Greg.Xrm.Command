using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
    [Command("column", "delete", HelpText = "Delete a column on a given Dataverse table")]
    [Alias("delete", "column")]
    public class DeleteCommand
    {
        [Option("table", "t", HelpText = "The name of the entity for which you want to create an attribute")]
        [Required]
        public string? EntityName { get; set; }


        [Option("schemaName", "sn", HelpText = "The schema name of the attribute.\nIf not specified, is deducted from the display name")]
        public string? SchemaName { get; set; }
    }
}
