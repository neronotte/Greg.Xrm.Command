using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "optionset", HelpText = "Creates an optionset (picklist) column.")]
	[Alias("column", "add", "picklist")]
	[Alias("column", "add", "choice")]
	[Alias("column", "add", "choices")]
	public class CreatePicklistCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("globalOptionSetName", "gon",
			HelpText = "For Picklist type columns that must be tied to a global option set,\nprovides the name of the global option set.",
			Order = 10)]
		public string? GlobalOptionSetName { get; set; }


		[Option("options", "o",
			HelpText = "The list of options for the attribute, as a single string separated by comma (,) or semicolon (;) or pipe.\nYou can pass also values separating using syntax \"label1:value1,label2:value2\"\nIf not provided, values will be automatically generated",
			Order = 20
		)]
		public string? Options { get; internal set; }


		[Option("defaultValue", "dv", HelpText = "For Picklist type columns indicates the default value for the column. You can provide the name or the value. If not provided, is automatically evaluated by the system.",
			Order = 30)]
		public string? DefaultFormValue { get; set; }

		[Option("multiselect", "m",
			HelpText = "Indicates whether the attribute is a multi-select picklist (default: false).",
			DefaultValue = false,
			Order = 40)]
		public bool Multiselect { get; set; } = false;



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer
				.WriteLine("This type of column is used for storing a single choice from a predefined list of options. You can create a simple picklist with options, or use an existing global option set.")
				.WriteLine("If you want to create a local option set column you can:")
				.WriteLine();

			writer.WriteList(
				"Specify only the options labels, separated by commas, semicolons or pipes (|). The system will automatically generate the values for you.",
				"Specify the options as \"label1:value1,label2:value2\" to create a picklist with custom values."
			);

			writer.WriteParagraph("As of now, you cannot specify a color for the picklist options.");

			writer.WriteParagraph("Please note that if you specify the values, values must be specified for all options, and they must be unique. If you don't specify the values, the system will generate them automatically starting from the Publisher OptionSetPrefix + 0000.");

			writer.WriteParagraph("If you want to create a multi-select picklist, you can use the `--multiselect` option. If you want to use an existing global option set, you can use the `--globalOptionSetName` option.");

			writer.WriteCodeBlock(@"# Creates a simple picklist with options
pacx column create --type Picklist -t tableName -n columnName --options ""Option 1,Option 2,Option 3""

# Create picklist with custom values
pacx column create --type Picklist -t tableName -n columnName --options ""Red:100000000,Green:100000001,Blue:100000002""

# Create multi-select picklist
pacx column create --type Picklist -t tableName -n columnName --options ""Tag1,Tag2,Tag3"" --multiselect

# Use existing global option set
pacx column create --type Picklist -t tableName -n columnName --globalOptionSetName existing_global_optionset", "Powershell");

			writer.WriteParagraph("You can also specify a default value for the picklist using the `--defaultValue` option. You can provide either the label or the value of the option (labels are matched first).");

			writer.WriteCodeBlock(@"# Create picklist with default value by label
pacx column create --type Picklist -t tableName -n columnName --options ""Red:100000000,Green:100000001,Blue:100000002"" --defaultValue Green
# Create picklist with default value by value
pacx column create --type Picklist -t tableName -n columnName --options ""Red,Green,Blue"" --defaultValue 100000001 # Green
pacx column create --type Picklist -t tableName -n columnName --options ""Red:100000000,Green:100000001,Blue:100000002"" --defaultValue 100000001
", "Powershell");
		}
	}
}
