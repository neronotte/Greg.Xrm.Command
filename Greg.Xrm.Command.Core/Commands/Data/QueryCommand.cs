using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Data
{
	[Command("data", "query", HelpText = "Commands to streamline data manipulation")]
	public class QueryCommand : IValidatableObject, ICanProvideUsageExample
	{
[Option("query", "q", HelpText = "The query to execute. Can be a FetchXML or SQL query. Mutually exclusive with --query-file.")]
		public string? Query { get; set; }

		[Option("query-file", "qf", HelpText = "Path to a file containing the query to execute. Mutually exclusive with --query.")]
		public string? QueryFile { get; set; }

		[Option("format", "f", HelpText = "The format of the output.", DefaultValue = OutputFormats.JSON)]
		public OutputFormats OutputFormat { get; set; }

		[Option("output", "o", HelpText = "The path to the output file. If not provided, the output will be printed to the console. If the format is Excel, this argument is mandatory.")]
		public string? OutputFileName { get; set; }

		[Option("auto-run", "run", HelpText = "If set, the output file will be automatically opened after the query is executed. Only applicable when an output file is specified.", DefaultValue = false)]
		public bool OutputFileAutoRun { get; set; } = false;



		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Query) && string.IsNullOrWhiteSpace(QueryFile))
			{
				yield return new ValidationResult("Either Query or QueryFile must be provided.", [nameof(Query), nameof(QueryFile)]);
			}
			else if (!string.IsNullOrWhiteSpace(Query) && !string.IsNullOrWhiteSpace(QueryFile))
			{
				yield return new ValidationResult("Only one of Query or QueryFile can be provided, not both.", [nameof(Query), nameof(QueryFile)]);
			}

			if (!string.IsNullOrWhiteSpace(QueryFile) && !File.Exists(QueryFile))
			{
				yield return new ValidationResult($"The specified query file '{QueryFile}' does not exist.", [nameof(QueryFile)]);
			}

			if (OutputFormat == OutputFormats.Excel && string.IsNullOrWhiteSpace(OutputFileName))
			{
				yield return new ValidationResult("The output file name must be provided when the format is Excel.", [nameof(OutputFileName)]);
			}

			if (!string.IsNullOrWhiteSpace(OutputFileName))
			{
				var file = new FileInfo(OutputFileName);
				var extension = Path.GetExtension(file.FullName);

				if (OutputFormat == OutputFormats.JSON && !".json".Equals(extension, StringComparison.OrdinalIgnoreCase))
				{
					yield return new ValidationResult("The output file extension does not match the selected output format.", [nameof(OutputFileName)]);
				}
				if (OutputFormat == OutputFormats.CSV && !".csv".Equals(extension, StringComparison.OrdinalIgnoreCase))
				{
					yield return new ValidationResult("The output file extension does not match the selected output format.", [nameof(OutputFileName)]);
				}
				if (OutputFormat == OutputFormats.XML && !".xml".Equals(extension, StringComparison.OrdinalIgnoreCase))
				{
					yield return new ValidationResult("The output file extension does not match the selected output format.", [nameof(OutputFileName)]);
				}
				if (OutputFormat == OutputFormats.Excel && !".xlsx".Equals(extension, StringComparison.OrdinalIgnoreCase))
				{
					yield return new ValidationResult("The output file extension does not match the selected output format.", [nameof(OutputFileName)]);
				}
			}
		}

		public enum OutputFormats
		{
			JSON,
			CSV,
			XML,
			Excel
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteTitle3("Overview");
			writer.WriteParagraph("This command allows you to execute queries against a Dataverse environment and retrieve data in various formats. It supports multiple query languages and output formats.");

			writer.WriteTitle3("Supported Query Types");
			writer.WriteParagraph("The command automatically detects the query type based on the query text:");
			writer.WriteList(
				"FetchXML: queries starting with '<' are interpreted as FetchXML",
				"SQL: queries starting with 'SELECT ' (case-insensitive) are interpreted as SQL",
				"OData: queries containing OData query options (e.g., $filter=, $select=, $top=) are interpreted as OData");

writer.WriteLine("> **Please note**: If you're using PowerShell, remember to escape the $ sign using the ` character in OData queries.");

			writer.WriteTitle3("Input Options");
			writer.WriteParagraph("You can provide the query in two mutually exclusive ways:");
			writer.WriteList(
				"--query (-q): pass the query text directly as a command argument",
				"--query-file (-qf): specify a path to a file containing the query text");

			writer.WriteTitle3("Output Formats");
			writer.WriteParagraph("The command supports multiple output formats via the ")
				.WriteCode("--format")
				.Write(" option:");
			writer.WriteList(
				"JSON (default): outputs the results as formatted JSON",
				"CSV: outputs the results as comma-separated values",
				"XML: outputs the results as XML",
				"Excel: outputs the results as an Excel file (.xlsx) - requires --output to be specified");

			writer.WriteTitle3("Output Destination");
			writer.WriteParagraph("By default, results are printed to the console. Use ")
				.WriteCode("--output (-o)")
				.Write(" to save results to a file. The file extension must match the selected format.");
			writer.WriteParagraph("Use ")
				.WriteCode("--auto-run")
				.Write(" to automatically open the output file after the query completes.");

			writer.WriteTitle3("Internal Processing");
			writer.WriteParagraph("For OData queries, the command performs special processing on the response:");
			writer.WriteList(
				"Properties in the form '_fieldname_value' are recognized as EntityReference (lookup) attributes",
				"Properties ending with '@OData.Community.Display.V1.FormattedValue' are extracted as FormattedValues",
				"Integer attributes with a corresponding FormattedValue are treated as OptionSetValue (choice fields)");

			
			writer.WriteTitle3("Examples");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# Execute a FetchXML query and display results in console");
			writer.WriteLine("pacx data query -q \"<fetch><entity name='account'><attribute name='name'/></entity></fetch>\"");
			writer.WriteLine();
			writer.WriteLine("# Execute a SQL query");
			writer.WriteLine("pacx data query -q \"SELECT name, accountid FROM account WHERE statecode = 0\"");
			writer.WriteLine();
			writer.WriteLine("# Execute an OData query");
			writer.WriteLine("pacx data query -q \"accounts?$select=name,accountid&$filter=statecode eq 0&$top=10\"");
			writer.WriteLine();
			writer.WriteLine("# Load query from file and export to JSON");
			writer.WriteLine("pacx data query --query-file ./queries/my-query.xml --output ./results/output.json");
			writer.WriteLine();
			writer.WriteLine("# Export to Excel and auto-open the file");
			writer.WriteLine("pacx data query -q \"<fetch><entity name='contact'/></fetch>\" -f Excel -o ./contacts.xlsx --auto-run");
			writer.WriteLine();
			writer.WriteLine("# Export to CSV");
			writer.WriteLine("pacx data query -q \"SELECT fullname, emailaddress1 FROM contact\" -f CSV -o ./contacts.csv");
			writer.WriteCodeBlockEnd();

			}
	}
}
