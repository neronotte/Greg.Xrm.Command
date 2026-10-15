using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "translate", HelpText = "Sets localized labels for an existing option set value.")]
	public class TranslateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The schema name of a global option set.")]
		public string Name { get; set; } = string.Empty;

		[Option("table", "t", Order = 2, HelpText = "The logical name of the table containing a local choice.")]
		public string TableName { get; set; } = string.Empty;

		[Option("column", "c", Order = 3, HelpText = "The logical name of the local choice column.")]
		public string ColumnName { get; set; } = string.Empty;

		[Option("value", "v", Order = 4, HelpText = "The numeric value of the option.")]
		[Required]
		public int Value { get; set; }

		[Option("labels", "l", Order = 5, HelpText = "Localized labels in the format LCID:\"label\".")]
		[Required]
		public string? Labels { get; set; }

		[Option("no-publish", "np", Order = 50, DefaultValue = false, HelpText = "Skips publishing the metadata changes.")]
		public bool NoPublish { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Name) &&
				(string.IsNullOrWhiteSpace(TableName) || string.IsNullOrWhiteSpace(ColumnName)))
			{
				yield return new ValidationResult(
					"Either 'name' or both 'table' and 'column' must be provided.",
					[nameof(Name), nameof(TableName), nameof(ColumnName)]);
			}

			if (!string.IsNullOrWhiteSpace(Name) &&
				(!string.IsNullOrWhiteSpace(TableName) || !string.IsNullOrWhiteSpace(ColumnName)))
			{
				yield return new ValidationResult(
					"'name' cannot be combined with 'table' or 'column'.",
					[nameof(Name), nameof(TableName), nameof(ColumnName)]);
			}

			if ("statecode".Equals(ColumnName, StringComparison.OrdinalIgnoreCase))
			{
				yield return new ValidationResult(
					"StateCode translations are not supported.",
					[nameof(ColumnName)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set localized labels on an option set value:");
			writer.WriteCodeBlock(
				"pacx optionset translate --name new_status --value 100000000 --labels '1033:\"Pending\", 1040:\"In attesa\"'",
				"Powershell");
		}
	}
}
