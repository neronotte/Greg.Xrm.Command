using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Data.Upsert
{
	[Command("data", "upsert", HelpText = "Creates or updates a record in a Dataverse table using an alternate key (upsert).")]
	public class UpsertCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", HelpText = "Logical name of the target table.")]
		[Required]
		public string? Table { get; set; }

		[Option("key", "k", HelpText = "Semicolon-separated list of field=value pairs that form the alternate key used to identify the record.")]
		[Required]
		public string? Key { get; set; }

		[Option("plain", "p", HelpText = "Semicolon-separated list of field=value pairs for the record payload. Mutually exclusive with --json and --file.")]
		public string? Plain { get; set; }

		[Option("json", "j", HelpText = "JSON string representing the record payload. Mutually exclusive with --plain and --file.")]
		public string? Json { get; set; }

		[Option("file", "f", HelpText = "Path to a JSON file containing the record payload. Mutually exclusive with --plain and --json.")]
		public string? File { get; set; }

		[Option("return", "r", HelpText = "Comma-separated list of columns to return after the operation. If omitted, only the record ID is returned.")]
		public string? Return { get; set; }

		[Option("dry-run", "dr", HelpText = "Validates the payload without performing the upsert.", DefaultValue = false)]
		public bool DryRun { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Key))
			{
				yield return new ValidationResult(
					"The --key option is required and must contain at least one field=value pair.",
					[nameof(Key)]);
			}

			var provided = new[]
			{
				!string.IsNullOrWhiteSpace(Plain),
				!string.IsNullOrWhiteSpace(Json),
				!string.IsNullOrWhiteSpace(File)
			};
			var count = provided.Count(x => x);

			if (count == 0)
			{
				yield return new ValidationResult(
					"Exactly one of --plain, --json, or --file must be provided.",
					[nameof(Plain), nameof(Json), nameof(File)]);
			}
			else if (count > 1)
			{
				yield return new ValidationResult(
					"Only one of --plain, --json, or --file can be provided, not multiple.",
					[nameof(Plain), nameof(Json), nameof(File)]);
			}

			if (!string.IsNullOrWhiteSpace(File) && !System.IO.File.Exists(File))
			{
				yield return new ValidationResult(
					$"The specified file '{File}' does not exist.",
					[nameof(File)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteTitle3("Overview");
			writer.WriteParagraph("This command performs an upsert (insert-or-update) operation on a Dataverse table. It uses an alternate key to determine whether the target record already exists: if it does, the record is updated; otherwise, a new record is created. The operation is idempotent and can be safely repeated.");

			writer.WriteTitle3("Input modes");
			writer.WriteParagraph("Exactly one of the following input modes must be used (they are mutually exclusive):");
			writer.WriteList(
				"--plain (-p): semicolon-separated list of field=value pairs, e.g. name=Contoso Ltd;telephone1=+39 02 1234567",
				"--json (-j): a JSON object string representing the record payload",
				"--file (-f): path to a JSON file containing the record payload");

			writer.WriteTitle3("Alternate key (--key)");
			writer.WriteParagraph("The --key option specifies the alternate key used to identify the record. It uses the same semicolon-separated field=value format as --plain:");
			writer.WriteList(
				"Single-field key: --key accountnumber=ACC001",
				"Multi-field key: --key \"field1=value1;field2=value2\"");
			writer.WriteParagraph("The key fields are used exclusively for record lookup. To also set the key field values in the payload, include them in --plain/--json/--file as well.");

			writer.WriteTitle3("Supported field types");
			writer.WriteTable(
				new[]
				{
					("String / Memo", "Any text value", "name=Contoso Ltd"),
					("Integer", "Whole number", "numberofemployees=100"),
					("Decimal / Double", "Decimal number", "revenue=1234.56"),
					("Money", "Monetary value", "estimatedvalue=50000"),
					("Boolean", "true/false, 1/0", "donotbulkemail=true"),
					("Date Only", "yyyy-MM-dd", "birthdate=1990-05-20"),
					("Date & Time", "ISO 8601", "overriddencreatedon=2024-01-15T08:30:00Z"),
					("Choice (OptionSet)", "Integer code or label text", "statecode=0 or statecode=Active"),
					("Multi-Select Choice", "Comma-separated codes or labels", "new_tags=Red,Blue,Green"),
					("Lookup", "entity(GUID) or entity(field='value')", "ownerid=systemuser(domainname='user@org.com')"),
				},
				["Type", "Format", "Example"],
				row => [row.Item1, row.Item2, row.Item3]);

			writer.WriteTitle3("Options");
			writer.WriteList(
				"--key (-k): required; semicolon-separated field=value pairs for the alternate key",
				"--return (-r): comma-separated list of columns to retrieve after the operation and display; if omitted only the record ID is shown",
				"--dry-run (-dr): validates and displays the resolved payload without actually performing the upsert");

			writer.WriteTitle3("Output");
			writer.WriteParagraph("The command reports whether the operation resulted in a record creation or an update, and returns the record ID.");

			writer.WriteTitle3("Examples");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# Upsert an account by alternate key");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --plain \"name=Contoso Ltd\"");
			writer.WriteLine();
			writer.WriteLine("# Upsert with multiple fields");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --plain \"name=Contoso Ltd;telephone1=+39 02 1234567\"");
			writer.WriteLine();
			writer.WriteLine("# Upsert and return specific fields");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --plain \"name=Contoso Ltd\" --return \"name,telephone1\"");
			writer.WriteLine();
			writer.WriteLine("# Upsert using JSON payload");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --json '{\"name\":\"Contoso Ltd\",\"telephone1\":\"+39 02 1234567\"}'");
			writer.WriteLine();
			writer.WriteLine("# Upsert from a JSON file");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --file ./account-payload.json");
			writer.WriteLine();
			writer.WriteLine("# Dry-run: validate the payload without upserting");
			writer.WriteLine("pacx data upsert -t account --key accountnumber=ACC001 --plain \"name=Contoso Ltd\" --dry-run");
			writer.WriteLine();
			writer.WriteLine("# Multi-field alternate key");
			writer.WriteLine("pacx data upsert -t new_config --key \"new_category=pricing;new_region=EMEA\" --plain \"new_value=standard\"");
			writer.WriteCodeBlockEnd();
		}
	}
}
