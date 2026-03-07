using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "datetime", HelpText = "Creates a datetime column.")]
	[Alias("column", "add", "date")]
	[Alias("column", "add", "time")]
	public class CreateDateTimeCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("behavior", "dtb", Order = 10, HelpText = "For DateTime type columns indicates the DateTimeBehavior of the column.", DefaultValue = DateTimeBehavior1.UserLocal)]
		public DateTimeBehavior1 DateTimeBehavior { get; set; } = DateTimeBehavior1.UserLocal;


		[Option("format", "dtf", Order = 11, HelpText = "For DateTime type columns indicates the DateTimeFormat of the column.", DefaultValue = DateTimeFormat.DateAndTime)]
		public DateTimeFormat DateTimeFormat { get; set; } = DateTimeFormat.DateAndTime;



		[Option("imeMode", "ime", Order = 20, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock(@"# Creates a simple date and time column
pacx column create --type DateTime -t tableName -n columnName

# Create date-only column
pacx column create --type DateTime -t tableName -n columnName --dateTimeBehavior DateOnly --dateTimeFormat DateOnly
pacx column create --type DateTime -t tableName -n columnName -dtb DateOnly -dtf DateOnly

# Create time-zone independent datetime
pacx column create --type DateTime -t tableName -n columnName --dateTimeBehavior TimeZoneIndependent

# Create user local datetime with date and time format
pacx column create --type DateTime -t tableName -n columnName --dateTimeBehavior UserLocal --dateTimeFormat DateAndTime", "Powershell");
		}
	}
}
