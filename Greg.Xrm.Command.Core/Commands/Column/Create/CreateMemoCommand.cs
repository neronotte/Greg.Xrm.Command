using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "memo", HelpText = "Creates a memo column.")]
	public class CreateMemoCommand : BaseCreateCommand
	{
		[Option("memoFormat", "mf", Order = 10, HelpText = "The format of the memo attribute (default: Text).", DefaultValue = MemoFormatName1.Text)]
		public MemoFormatName1 MemoFormat { get; set; } = MemoFormatName1.Text;

		[Option("len", "l", Order = 11, HelpText = "The maximum length for string attribute.", DefaultValue = 2000)]
		public int? MaxLength { get; set; }

		[Option("imeMode", "ime", Order = 40, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;
	}
}
