using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "delete", HelpText = "Deletes a table (if possible) from the current Dataverse environment")]
	[Alias("delete", "table")]
	public class DeleteCommand
	{
		[Option("name", "n", HelpText = "The schema name of the table to delete")]
		[Required]
		public string? SchemaName { get; set; }
	}
}
