using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "money", HelpText = "Creates a money column.")]
	[Alias("column", "add", "currency")]
	public class CreateMoneyCommand : BaseCreateCommand
	{
		[Option("min", "min", Order = 10, HelpText = "For number type columns indicates the minimum value for the column.")]
		public double? MinValue { get; set; }

		[Option("max", "max", Order = 11, HelpText = "For number type columns indicates the maximum value for the column.")]
		public double? MaxValue { get; set; }

		[Option("precision", "p", Order = 12, HelpText = "For money or decimal type columns indicates the precision for the column.", DefaultValue = 2)]
		public int? Precision { get; set; }

		[Option("precisionSource", "ps", Order = 13, HelpText = "Indicates if precision should be taken from:\n(0) the precision property,\n(1) the `Organization.PricingDecimalPrecision` attribute or\n(2) the `TransactionCurrency.CurrencyPrecision` property of the transaction currency that is associated the current record.\n", DefaultValue = 2)]
		public int? PrecisionSource { get; set; }

		[Option("imeMode", "ime", Order = 20, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;
	}
}
