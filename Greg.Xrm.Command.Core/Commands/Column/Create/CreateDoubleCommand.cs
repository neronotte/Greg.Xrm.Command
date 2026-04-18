using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "double", HelpText = "Creates a double column.")]
	public class CreateDoubleCommand : BaseCreateCommand
	{
		[Option("min", "min", Order = 10, HelpText = "For number type columns indicates the minimum value for the column.")]
		public double? MinValue { get; set; }

		[Option("max", "max", Order = 11, HelpText = "For number type columns indicates the maximum value for the column.")]
		public double? MaxValue { get; set; }

		[Option("precision", "p", Order = 12, HelpText = "For money or decimal type columns indicates the precision for the column.", DefaultValue = 2)]
		public int? Precision { get; set; }

		[Option("imeMode", "ime", Order = 20, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;
	}
}
