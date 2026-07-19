using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Data.Delete
{
	[Command("data", "delete", HelpText = "Deletes a record from a Dataverse table.")]
	public class DeleteCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", HelpText = "Logical name of the target table.")]
		[Required]
		public string? Table { get; set; }

		[Option("id", "id", HelpText = "The GUID of the record to delete.")]
		[Required]
		public Guid Id { get; set; }

		[Option("dry-run", "dr", HelpText = "Retrieves and displays the record without deleting it.", DefaultValue = false)]
		public bool DryRun { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Id == Guid.Empty)
			{
				yield return new ValidationResult(
					"The --id option must be a non-empty GUID.",
					[nameof(Id)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteTitle3("Overview");
			writer.WriteParagraph("This command deletes a record from the specified Dataverse table. Both --table and --id are required. Use --dry-run to preview the record before committing the deletion.");

			writer.WriteTitle3("Options");
			writer.WriteList(
				"--table (-t): logical name of the table containing the record",
				"--id: GUID of the record to delete",
				"--dry-run (-dr): retrieves and displays the record's primary field without deleting it");

			writer.WriteTitle3("Examples");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# Delete a contact record");
			writer.WriteLine("pacx data delete -t contact --id 3fa85f64-5717-4562-b3fc-2c963f66afa6");
			writer.WriteLine();
			writer.WriteLine("# Preview the record before deleting");
			writer.WriteLine("pacx data delete -t contact --id 3fa85f64-5717-4562-b3fc-2c963f66afa6 --dry-run");
			writer.WriteCodeBlockEnd();
		}
	}
}
