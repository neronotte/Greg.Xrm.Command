using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Key
{
	[Command("key", "create", HelpText = "Creates an alternative key on a given table.")]
	[Alias("key", "add")]
	public class CreateCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table where the key must be set.")]
		[Required]
		public string Table { get; set; } = string.Empty;

		[Option("columns", "c", Order = 2, HelpText = "The logical names of the columns that should be part of the key, separated by commas.")]
		[Required]
		public string Columns { get; set; } = string.Empty;

		[Option("name", "n", Order = 3, HelpText = "The display name of the key. If not provided, a name will be generated based on the table name.")]
		public string DisplayName { get; set; } = string.Empty;

		[Option("schemaName", "sn", Order = 4, HelpText = "The schema name of the key. If not provided, a name will be generated based on the display name (Format: publisher prefix, underscore, display name all lowercase without spaces and special characters).")]
		public string SchemaName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 5, HelpText = "The name of the solution where the key will be created. If not provided, the default solution will be used.")]
		public string? SolutionName { get; set; }



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("You can use both forms: ");

			writer.WriteCodeBlock(@"
pacx key create --table account --columns accountnumber,telephone1
pacx key add --table account --columns accountnumber,telephone1", "Powershell");

			writer.WriteParagraph("If you don't specify a **display name**, it will be inferred automatically using the following convention: `<table logical name> + '_key'`");

			writer.WriteLine("If you don't specify a **schema name**, it will be inferred automatically using the following convention: `<publisher prefix> + '_' + <display name all lowercase without spaces and special characters>`");
			writer.WriteParagraph("If the table schema name already starts with the publisher prefix, it won't be duplicated. E.g.:");

			writer.WriteCodeBlock(@"
pacx key create --table account ...        # -->  DisplayName: account_key, Schema Name: new_account_key
pacx key create --table new_something ...  # -->  DisplayName: new_something_key, Schema Name: new_something_key", "Powershell");

			writer.WriteParagraph("You can specify the unique name of the solution where the key will be created. If not provided, the default solution will be used.");
		}
	}
}
