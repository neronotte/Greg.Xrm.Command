using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "translate", HelpText = "Sets localized metadata and value labels for an existing option set.")]
	public class TranslateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The schema name of a global option set.")]
		public string Name { get; set; } = string.Empty;

		[Option("table", "t", Order = 2, HelpText = "The logical name of the table containing a local choice.")]
		public string TableName { get; set; } = string.Empty;

		[Option("column", "c", Order = 3, HelpText = "The logical name of the local choice column.")]
		public string ColumnName { get; set; } = string.Empty;

		[Option("value", "v", Order = 4, HelpText = "The numeric value of the option.")]
		public int? Value { get; set; }

		[Option("labels", "l", Order = 5, HelpText = "Localized labels in the format LCID:\"label\".")]
		public string? Labels { get; set; }

		[Option("displayName", "dn", Order = 6, HelpText = "Localized display names for a global option set.")]
		public string? DisplayName { get; set; }

		[Option("description", "d", Order = 7, HelpText = "Localized descriptions for a global option set.")]
		public string? Description { get; set; }

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

			if ("statecode".Equals(ColumnName, StringComparison.OrdinalIgnoreCase) &&
				!string.IsNullOrWhiteSpace(Name))
			{
				yield return new ValidationResult(
					"StateCode must be addressed through a table and column.",
					[nameof(Name), nameof(TableName), nameof(ColumnName)]);
			}

			if (Value.HasValue != !string.IsNullOrWhiteSpace(Labels))
			{
				yield return new ValidationResult(
					"'value' and 'labels' must be provided together.",
					[nameof(Value), nameof(Labels)]);
			}

			if (string.IsNullOrWhiteSpace(Name) &&
				(!string.IsNullOrWhiteSpace(DisplayName) || !string.IsNullOrWhiteSpace(Description)))
			{
				yield return new ValidationResult(
					"'displayName' and 'description' can only be used with a global option set.",
					[nameof(Name), nameof(DisplayName), nameof(Description)]);
			}

			if (!Value.HasValue &&
				string.IsNullOrWhiteSpace(DisplayName) &&
				string.IsNullOrWhiteSpace(Description))
			{
				yield return new ValidationResult(
					"Provide either 'value' and 'labels', or a global option set 'displayName' or 'description'.",
					[nameof(Value), nameof(Labels), nameof(DisplayName), nameof(Description)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set localized labels on an option set value:");
			writer.WriteCodeBlock(
				"pacx optionset translate --name new_status --value 100000000 --labels '1033:\"Pending\", 1040:\"In attesa\"'",
				"Powershell");
			writer.WriteParagraph("Set the localized display name and description of a global option set:");
			writer.WriteCodeBlock(
				"pacx optionset translate --name new_status --displayName '1033:\"Contract status\", 1040:\"Stato contratto\"' --description '1033:\"Available statuses\", 1040:\"Stati disponibili\"'",
				"Powershell");
		}
	}
}
