using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Key
{
	[Command("key", "delete", HelpText = "Removes an alternative key from a given table.")]
	[Alias("key", "remove")]
	[Alias("key", "del")]
	[Alias("key", "rm")]
	public class DeleteCommand
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table where the key is defined.")]
		[Required]
		public string Table { get; set; } = string.Empty;

		[Option("schemaName", "sn", Order = 2, HelpText = "The schema name of the key to deletes.")]
		[Required]
		public string SchemaName { get; set; } = string.Empty;
	}

}
