using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "decimal", HelpText = "Creates a decimal column.")]
	public class CreateDecimalCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("min", "min", Order = 10, HelpText = "For number type columns indicates the minimum value for the column.")]
		public double? MinValue { get; set; }

		[Option("max", "max", Order = 11, HelpText = "For number type columns indicates the maximum value for the column.")]
		public double? MaxValue { get; set; }

		[Option("precision", "p", Order = 12, HelpText = "For money or decimal type columns indicates the precision for the column.", DefaultValue = 2)]
		public int? Precision { get; set; }

		[Option("imeMode", "ime", Order = 20, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("This type of column is used for storing decimal numbers with a specified precision and range.");
			writer.WriteLine("If you specify \"Decimal\" as the type, the system will automatically generate a column that in the maker UI is shown as DataType=Decimal.");
			writer.WriteLine("If you specify \"Double\" as the type, the system will automatically generate a column that in the maker UI is shown as DataType=Float.");
			writer.WriteLine();

			writer.WriteCodeBlock(@"# Creates a simple decimal column with precision 2
pacx column create --type Decimal -t tableName -n columnName
pacx column create --type Double -t tableName -n columnName

# Set precision and min/max values
pacx column create --type Decimal -t tableName -n columnName --precision 4 --min 0 --max 999.99
pacx column create --type Double -t tableName -n columnName --precision 4 --min 0 --max 999.99", "Powershell");
		}
	}
}
