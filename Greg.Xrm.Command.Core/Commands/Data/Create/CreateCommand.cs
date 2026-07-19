using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Data.Create
{
	[Command("data", "create", HelpText = "Creates a record in a Dataverse table.")]
	public class CreateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", HelpText = "Logical name of the target table.")]
		[Required]
		public string? Table { get; set; }

		[Option("plain", "p", HelpText = "Semicolon-separated list of field=value pairs. Mutually exclusive with --json and --file.")]
		public string? Plain { get; set; }

		[Option("json", "j", HelpText = "JSON string representing the record payload. Mutually exclusive with --plain and --file.")]
		public string? Json { get; set; }

		[Option("file", "f", HelpText = "Path to a JSON file containing the record payload. Mutually exclusive with --plain and --json.")]
		public string? File { get; set; }

		[Option("id", HelpText = "Optional GUID to assign to the new record.")]
		public Guid? Id { get; set; }

		[Option("return", "r", HelpText = "Comma-separated list of columns to return after creation. If omitted, only the record ID is returned.")]
		public string? Return { get; set; }

		[Option("dry-run", "dr", HelpText = "Validates the payload without creating the record.", DefaultValue = false)]
		public bool DryRun { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
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
			writer.WriteParagraph("This command creates a new record in the specified Dataverse table. You can provide field values using a plain key=value syntax, a JSON string, or a JSON file.");

			writer.WriteTitle3("Input modes");
			writer.WriteParagraph("Exactly one of the following input modes must be used (they are mutually exclusive):");
			writer.WriteList(
				"--plain (-p): semicolon-separated list of field=value pairs, e.g. firstname=Mario;lastname=Rossi",
				"--json (-j): a JSON object string representing the record payload",
				"--file (-f): path to a JSON file containing the record payload");

			writer.WriteTitle3("Supported field types");
			writer.WriteTable(
				new[]
				{
					("String / Memo", "Any text value", "firstname=Mario"),
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

			writer.WriteTitle3("Lookup references");
			writer.WriteParagraph("Lookup fields accept two formats:");
			writer.WriteList(
				"entity(GUID): directly references a record by its GUID, e.g. ownerid=systemuser(3fa85f64-5717-4562-b3fc-2c963f66afa6)",
				"entity(field='value'): resolves a record by a field value, e.g. ownerid=systemuser(domainname='mario.rossi@contoso.com')");
			writer.WriteParagraph("If the field value itself contains a single quote, escape it by doubling it: entity(name='Riccardo''s Corp').");

			writer.WriteTitle3("--plain escaping rules");
			writer.WriteParagraph("The --plain tokenizer uses the following rules:");
			writer.WriteList(
				"Fields are separated by semicolons (;)",
				"The first = in each token separates the field name from the value; subsequent = characters are part of the value",
				"Single quotes (') delimit sections where semicolons are treated as literal characters",
				"To include a literal single quote in a value, write two consecutive single quotes ('')",
				"An empty value (field=) is valid and sets the field to null");

			writer.WriteTitle3("Options");
			writer.WriteList(
				"--id: optional GUID to assign to the new record; if omitted Dataverse generates one",
				"--return (-r): comma-separated list of columns to retrieve after creation and display; if omitted only the record ID is shown",
				"--dry-run (-dr): validates and displays the resolved payload without actually creating the record");

			writer.WriteTitle3("Examples");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# Create a contact with simple values");
			writer.WriteLine("pacx data create -t contact --plain \"firstname=Mario;lastname=Rossi\"");
			writer.WriteLine();
			writer.WriteLine("# Create an account with a lookup resolved by field");
			writer.WriteLine("pacx data create -t account --plain \"name=Acme Corp;ownerid=systemuser(domainname='mario.rossi@contoso.com')\"");
			writer.WriteLine();
			writer.WriteLine("# Assign a predetermined GUID to the new record");
			writer.WriteLine("pacx data create -t account --plain \"name=Acme Corp\" --id 3fa85f64-5717-4562-b3fc-2c963f66afa6");
			writer.WriteLine();
			writer.WriteLine("# Create and return specific fields");
			writer.WriteLine("pacx data create -t contact --plain \"firstname=Mario;lastname=Rossi\" --return \"firstname,lastname,fullname\"");
			writer.WriteLine();
			writer.WriteLine("# Use --json (useful for AI pipelines)");
			writer.WriteLine("pacx data create -t opportunity --json '{\"name\":\"Big Deal\",\"estimatedvalue\":50000}'");
			writer.WriteLine();
			writer.WriteLine("# Create from a JSON file");
			writer.WriteLine("pacx data create -t contact --file ./new-contact.json");
			writer.WriteLine();
			writer.WriteLine("# Dry-run: validate the payload without creating");
			writer.WriteLine("pacx data create -t contact --plain \"firstname=Mario;birthdate=1990-05-20\" --dry-run");
			writer.WriteLine();
			writer.WriteLine("# Multi-select choice by label");
			writer.WriteLine("pacx data create -t new_survey --plain \"new_tags=Red,Blue,Green\"");
			writer.WriteLine();
			writer.WriteLine("# Polymorphic lookup (Customer)");
			writer.WriteLine("pacx data create -t incident --plain \"title=Support Case;customerid=account(accountnumber='ACME001')\"");
			writer.WriteLine();
			writer.WriteLine("# Clear a nullable field");
			writer.WriteLine("pacx data create -t contact --plain \"description=\"");
			writer.WriteLine();
			writer.WriteLine("# Value with single quote (escape with '')");
			writer.WriteLine("pacx data create -t account --plain \"name=Riccardo''s Corp\"");
			writer.WriteCodeBlockEnd();
		}
	}
}
