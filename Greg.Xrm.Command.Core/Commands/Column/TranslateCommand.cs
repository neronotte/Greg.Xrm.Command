using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "translate", HelpText = "Sets localized display names for an existing column.")]
	public class TranslateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table containing the column.")]
		[Required]
		public string? TableName { get; set; }

		[Option("column", "c", Order = 2, HelpText = "The logical name of the column to translate.")]
		[Required]
		public string? ColumnName { get; set; }

		[Option("labels", "l", Order = 3, HelpText = "Localized labels in the format LCID:\"label\", for example 1033:\"Customer\", 1040:\"Cliente\".")]
		public string? Labels { get; set; }

		[Option("description", "d", Order = 4, HelpText = "Localized descriptions in the format LCID:\"label\".")]
		public string? Description { get; set; }

		[Option("true-label", "tl", Order = 5, HelpText = "Localized True labels for Boolean columns.")]
		public string? TrueLabel { get; set; }

		[Option("false-label", "fl", Order = 6, HelpText = "Localized False labels for Boolean columns.")]
		public string? FalseLabel { get; set; }

		[Option("no-publish", "np", Order = 50, DefaultValue = false, HelpText = "Skips publishing the metadata changes.")]
		public bool NoPublish { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Labels) &&
				string.IsNullOrWhiteSpace(Description) &&
				string.IsNullOrWhiteSpace(TrueLabel) &&
				string.IsNullOrWhiteSpace(FalseLabel))
			{
				yield return new ValidationResult(
					"At least one of 'labels', 'description', 'true-label', or 'false-label' must be provided.",
					[nameof(Labels), nameof(Description), nameof(TrueLabel), nameof(FalseLabel)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set one or more localized labels on a column:");
			writer.WriteCodeBlock(
				"pacx column translate --table account --column new_name --labels '1033:\"Customer name\", 1040:\"Nome cliente\"'",
				"Powershell");
		}
	}
}
