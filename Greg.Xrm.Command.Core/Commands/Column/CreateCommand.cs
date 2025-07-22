using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
    [Command("column", "create", HelpText = "Creates a new column on a given Dataverse table")]
    [Alias("create", "column")]
    public class CreateCommand : ICanProvideUsageExample
    {
        [Option("table", "t", HelpText = "The name of the entity for which you want to create an attribute")]
        [Required]
        public string? EntityName { get; set; }

        [Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this attribute.")]
        public string? SolutionName { get; set; }

        [Option("name", "n", HelpText = "The display name of the attribute.")]
        [Required]
        public string? DisplayName { get; set; }

        [Option("schemaName", "sn", HelpText = "The schema name of the attribute.\nIf not specified, is deducted from the display name")]
        public string? SchemaName { get; set; }

        [Option("description", "d", HelpText = "The description of the attribute.")]
        public string? Description { get; set; }

        [Option("type", "at", HelpText = "The type of the attribute.\nCurrently supported values: Integer, Money, Picklist, String, DateTime, Memo, Boolean, Decimal.\n[default: String]", SuppressValuesHelp = true)]
        public AttributeTypeCode AttributeType { get; set; } = AttributeTypeCode.String;

        [Option("stringFormat", "sf", HelpText = "The format of the string attribute (default: Text).")]
        public StringFormat StringFormat { get; set; } = StringFormat.Text;

        [Option("memoFormat", "mf", HelpText = "The format of the memo attribute (default: Text).", DefaultValue = MemoFormatName1.Text)]
        public MemoFormatName1 MemoFormat { get; set; } = MemoFormatName1.Text;

        [Option("intFormat", "if", HelpText = "For whole number type columns indicates the integer format for the column.(default: None)")]
        public IntegerFormat IntegerFormat { get; set; } = IntegerFormat.None;

        [Option("requiredLevel", "r", HelpText = "The required level of the attribute.")]
        public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;

        [Option("len", "l", HelpText = "The maximum length for string attribute.")]
        public int? MaxLength { get; set; }

        [Option("autoNumber", "an", HelpText = "In case of autonumber field, the autonumber format to apply.")]
        public string? AutoNumber { get; set; }

        [Option("audit", "a", HelpText = "Indicates whether the attribute is enabled for auditing (default: true).")]
        public bool IsAuditEnabled { get; set; } = true;

        [Option("options", "o", HelpText = "The list of options for the attribute, as a single string separated by comma (,) or semicolon (;) or pipe.\nYou can pass also values separating using syntax \"label1=value1,label2=value2\"\nIf not provided, values will be automatically generated")]
        public string? Options { get; internal set; }

        [Option("globalOptionSetName", "gon", HelpText = "For Picklist type columns that must be tied to a global option set,\nprovides the name of the global option set.")]
        public string? GlobalOptionSetName { get; set; }

        [Option("multiselect", "m", HelpText = "Indicates whether the attribute is a multi-select picklist (default: false).", DefaultValue = false)]
        public bool Multiselect { get; set; } = false;

        [Option("min", "min", HelpText = "For number type columns indicates the minimum value for the column.")]
        public double? MinValue { get; set; }

        [Option("max", "max", HelpText = "For number type columns indicates the maximum value for the column.")]
        public double? MaxValue { get; set; }

        [Option("precision", "p", HelpText = "For money or decimal type columns indicates the precision for the column.", DefaultValue = 2)]
        public int? Precision { get; set; }

        [Option("precisionSource", "ps", HelpText = "For money type columns indicates if precision should be taken from:\n(0) the precision property,\n(1) the `Organization.PricingDecimalPrecision` attribute or\n(2) the `TransactionCurrency.CurrencyPrecision` property of the transaction currency that is associated the current record.\n", DefaultValue = 2)]
        public int? PrecisionSource { get; set; }

        [Option("imeMode", "ime", HelpText = "For number type columns indicates the input method editor (IME) mode for the column.", DefaultValue = ImeMode.Disabled)]
        public ImeMode ImeMode { get; set; } = ImeMode.Disabled;

        [Option("dateTimeBehavior", "dtb", HelpText = "For DateTime type columns indicates the DateTimeBehavior of the column.", DefaultValue = DateTimeBehavior1.UserLocal)]
        public DateTimeBehavior1 DateTimeBehavior { get; set; } = DateTimeBehavior1.UserLocal;

        [Option("dateTimeFormat", "dtf", HelpText = "For DateTime type columns indicates the DateTimeFormat of the column.", DefaultValue = DateTimeFormat.DateAndTime)]

        public DateTimeFormat DateTimeFormat { get; set; } = DateTimeFormat.DateAndTime;

        [Option("trueLabel", "tl", HelpText = "For Boolean type columns that represents the Label to be associated to the \"True\" value.", DefaultValue = "True")]
        public string? TrueLabel { get; set; } = "True";

        [Option("falseLabel", "fl", HelpText = "For  Boolean type columns that represents the Label to be associated to the \"False\" value.", DefaultValue = "False")]
        public string? FalseLabel { get; set; } = "False";

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("> This section is a work in progress");

			writer.WriteLine("This command allows you to create a new column on a given Dataverse table. You can specify various attributes of the column, such as its name, type, format, and other properties, and the command will automatically infer the remaining ones basing on the conventions described in the table below.");
			writer.WriteParagraph("The following sections describe how to generate each specific type of column.");



			WriteUsageString(writer);
            WriteUsageMemo(writer);
            WriteUsageBoolean(writer);

		}

		private static void WriteUsageString(MarkdownWriter writer)
		{
			writer.WriteTitle3("Text (String) column");
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
		}

		private static void WriteUsageMemo(MarkdownWriter writer)
		{
			writer.WriteTitle3("Multiline String (Memo) column");
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

		private static void WriteUsageBoolean(MarkdownWriter writer)
		{
			writer.WriteTitle3("True/False (Boolean) column");

			writer.WriteCodeBlock(@"# Creates a simple true/false column
pacx column create --type Boolean -t tableName -n columnName

# Change the labels for True and False values
pacx column create --type Boolean -t tableName -n columnName --trueValue Yes --falseValue No");
		}


	}

    public enum DateTimeBehavior1
    {
        UserLocal,
        TimeZoneIndependent,
        DateOnly
    }
    public enum MemoFormatName1
    {
        Email,
        Json,
        RichText,
        Text,
        TextArea
    }
}
