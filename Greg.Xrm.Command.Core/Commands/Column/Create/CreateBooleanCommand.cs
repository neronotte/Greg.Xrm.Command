using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "boolean", HelpText = "Creates a boolean column.")]
	[Alias("column", "add", "bool")]
	[Alias("column", "add", "yesno")]
	public class CreateBooleanCommand : BaseCreateCommand, ICanProvideUsageExample
	{

		[Option("trueLabel", "tl", Order = 10, HelpText = "For Boolean type columns that represents the Label to be associated to the \"True\" value.", DefaultValue = "True")]
		public string? TrueLabel { get; set; } = "True";

		[Option("falseLabel", "fl", Order = 11, HelpText = "For  Boolean type columns that represents the Label to be associated to the \"False\" value.", DefaultValue = "False")]
		public string? FalseLabel { get; set; } = "False";

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock(@"# Creates a simple true/false column
pacx column create --type Boolean -t tableName -n columnName

# Change the labels for True and False values
pacx column create --type Boolean -t tableName -n columnName --trueLabel Yes --falseLabel No", "Powershell");
		}
	}
}
