using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "delete", HelpText = "Deletes a column from a given Dataverse table.")]
	[Alias("delete", "column")]
	public class DeleteCommand
	{
		[Option("table", "t", HelpText = "The schema name of the table that contains the column to delete.")]
		[Required]
		public string? EntityName { get; set; }

		[Option("schemaName", "sn", HelpText = "The schema name of the column to delete.")]
		[Required]
		public string? SchemaName { get; set; }
	}
}
