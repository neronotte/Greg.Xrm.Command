using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "integer", HelpText = "Creates an integer column.")]
	[Alias("column", "add", "int")]
	[Alias("column", "add", "wholenumber")]
	public class CreateIntegerCommand : BaseCreateCommand
	{
		[Option("min", "min", Order = 10, HelpText = "For number type columns indicates the minimum value for the column.")]
		public double? MinValue { get; set; }

		[Option("max", "max", Order = 11, HelpText = "For number type columns indicates the maximum value for the column.")]
		public double? MaxValue { get; set; }

		[Option("intFormat", "if", Order = 13, HelpText = "For whole number type columns indicates the integer format for the column.(default: None)")]
		public IntegerFormat IntegerFormat { get; set; } = IntegerFormat.None;

		[Option("imeMode", "ime", Order = 20, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;
	}
}
