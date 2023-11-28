using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "delete", HelpText = "Deletes a column from a table.")]
	[Alias("delete", "column")]
	public class DeleteCommand
	{
		[Option("table", "t", HelpText = "The name of the entity for which you want to create an attribute.")]
		[Required]
		public string? EntityName { get; set; }

		[Option("schemaName", "sn", HelpText = "The schema name of the attribute.")]
		[Required]
		public string? SchemaName { get; set; }
	}
}
