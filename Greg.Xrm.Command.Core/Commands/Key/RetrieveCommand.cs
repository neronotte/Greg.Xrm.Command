using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Key
{
	[Command("key", "retrieve", HelpText = "Returns the definition of an alternative key from a given table.")]
	[Alias("key", "get")]
	public class RetrieveCommand
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table where the key is defined.")]
		[Required]
		public string Table { get; set; } = string.Empty;

		[Option("schemaName", "sn", Order = 2, HelpText = "The schema name of the key to retrieve.")]
		[Required]
		public string SchemaName { get; set; } = string.Empty;
	}
}
