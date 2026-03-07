using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "string", HelpText = "Creates a string column.")]
	[Alias("column", "add", "text")]
	public class CreateStringCommand : BaseCreateCommand
	{
		[Option("format", "f",  HelpText = "The format of the string attribute (default: Text).", Order = 10)]
		public StringFormat StringFormat { get; set; } = StringFormat.Text;

		[Option("len", "l", HelpText = "The maximum length for string attribute.", DefaultValue = 100, Order = 20)]
		public int? MaxLength { get; set; }

		[Option("autoNumber", "an", HelpText = "In case of autonumber field, the autonumber format to apply.", Order = 30)]
		public string? AutoNumber { get; set; }
	}
}
