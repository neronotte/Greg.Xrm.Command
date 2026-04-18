using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "memo", HelpText = "Creates a memo column.")]
	public class CreateMemoCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("memoFormat", "mf", Order = 10, HelpText = "The format of the memo attribute (default: Text).", DefaultValue = MemoFormatName1.Text)]
		public MemoFormatName1 MemoFormat { get; set; } = MemoFormatName1.Text;

		[Option("len", "l", Order = 11, HelpText = "The maximum length for string attribute.", DefaultValue = 2000)]
		public int? MaxLength { get; set; }

		[Option("imeMode", "ime", Order = 40, HelpText = "Indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("It's a different type of String column, that by default accepts more than one line of text");

			writer.WriteCodeBlock("pacx column create --type Memo -t tableName -n columnName", "Powershell");

			writer.WriteLine("The system will automatically generate a column of type Memo with the following features:").WriteLine();
			writer.WriteList(
				"**Display Name**: columnName"
				, "**Schema Name**: publisherprefix_columnName (all lowercase without special chars or spaces)"
				, "**Type**: Memo"
				, "**Format**: Text"
				, "**Max Length**: 2000"
				, "**Required**: None"
				, "**Audit Enabled**: true");

			writer.WriteParagraph("You can manually set all other arguments in the following way:");

			writer.WriteCodeBlock(@"# Specify a different format (Email, Json, RichText, Text, TextArea)
pacx column create --type Memo -t tableName -n columnName --memoFormat RichText

# Specify a different max length (default is 2000)
pacx column create --type Memo -t tableName -n columnName --len 200

# Create a required field (supported values are: None, ApplicationRequired, Recommended)
pacx column create --type Memo -t tableName -n columnName -r ApplicationRequired", "Powershell");
		}
	}
}
