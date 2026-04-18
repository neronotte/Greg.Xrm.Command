using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "string", HelpText = "Creates a string column.")]
	[Alias("column", "add", "text")]
	public class CreateStringCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("format", "f", HelpText = "The format of the string attribute (default: Text).", Order = 10)]
		public StringFormat StringFormat { get; set; } = StringFormat.Text;

		[Option("len", "l", HelpText = "The maximum length for string attribute.", DefaultValue = 100, Order = 20)]
		public int? MaxLength { get; set; }

		[Option("autoNumber", "an", HelpText = "In case of autonumber field, the autonumber format to apply.", Order = 30)]
		public string? AutoNumber { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("It's the type of column created by default if you simply type");

			writer.WriteCodeBlock("pacx column create -t tableName -n columnName", "Powershell");

			writer.WriteLine("The system will automatically generate a column of type text with the following features:").WriteLine();
			writer.WriteList(
				"**Display Name**: columnName"
				, "**Schema Name**: publisherprefix_columnName (all lowercase without special chars or spaces)"
				, "**Type**: String"
				, "**Format**: Text"
				, "**Max Length**: 100"
				, "**Required**: None"
				, "**Audit Enabled**: true");

			writer.WriteParagraph("You can manually set all other arguments in the following way:");

			writer.WriteCodeBlock(@"# Specify a different format (supported values are: Email, Text, TextArea, Url, TickerSymbol, Phone, Json, RichText)
pacx column create -t tableName -n columnName --stringFormat Email

# Specify a different max length (default is 100)
pacx column create -t tableName -n columnName --len 200

# Create a required field (supported values are: None, ApplicationRequired, Recommended)
pacx column create -t tableName -n columnName -r ApplicationRequired

# Disable auditing for this column
pacx column create -t tableName -n columnName --audit false

# Create a column with a description
pacx column create -t tableName -n columnName -d ""This is a description of the column""

# Create a column of type TextArea or RichText, required
pacx column create -t tableName -n columnName --stringFormat TextArea --len 2000 -r ApplicationRequired 
pacx column create -t tableName -n columnName --stringFormat RichText --len 2000 -r ApplicationRequired 

# Create a column of type Json
pacx column create -t tableName -n columnName --stringFormat Json --len 4000", "Powershell");


			writer.WriteParagraph("If you want to create an autonumber field, you can use the `--autoNumber` option. The format must be specified in the form of a string, using [the same syntax as the one used in the maker portal](https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/create-auto-number-attributes?view=op-9-1#autonumberformat-options). For example, you can use `{SEQNUM(5)}` to create a 5-digit autonumber field.");

			writer.WriteCodeBlock(@"# Example value: XX-00001
pacx column create -t tableName -n columnName --autonumber ""XX-{SEQNUM(5)}""

# Example value: 123456-#-R3V
pacx column create -t tableName -n columnName --autonumber ""{SEQNUM:6}-#-{RANDSTRING:3}""

# Example value: CAS-002000-S1P0H0-20170913091544
pacx column create -t tableName -n columnName --autonumber ""CAS-{SEQNUM:6}-{RANDSTRING:6}-{DATETIMEUTC:yyyyMMddhhmmss}""

# Example value: CAS-002000-201709-Z8M2Z6-110901
pacx column create -t tableName -n columnName --autonumber ""CAS-{SEQNUM:6}-{DATETIMEUTC:yyyyMM}-{RANDSTRING:6}-{DATETIMEUTC:hhmmss}""
", "Powershell");
		}
	}
}
