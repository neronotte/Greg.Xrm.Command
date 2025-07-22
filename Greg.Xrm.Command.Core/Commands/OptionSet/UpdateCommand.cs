using Greg.Xrm.Command.Model;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "update", HelpText = "Updates an existing value on an option set (global or local).")]
	public class UpdateCommand : IValidatableObject
	{
		[Option("name", "n", HelpText = "To be specified only if you want to update a global option set. It's the schema name of the global option set.")]
		public string Name { get; set; } = string.Empty;

		[Option("table", "t", HelpText = "To be specified only if you want to update a local option set. It's the schema name of the table that contains the option set.")]
		public string TableName { get; set; } = string.Empty;

		[Option("column", "c", HelpText = "To be specified only if you want to update a local option set. It's the schema name of the column that contains the option set.")]
		public string ColumnName { get; set; } = string.Empty;




		[Option("value", "v", HelpText = "The value of the option to update.")]
		[Required]
		public int Value { get; set; } = 0;

		[Option("label", "l", HelpText = "The new label to set on the option.")]
		public string? DisplayName { get; set; }

		[Option("color", "col", HelpText = "The exadecimal color code (e.g. #FF5733) of the new color to set on the option. The leading # is not mandatory.")]
		public string? Color { get; set; }



		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Name) &&
				(string.IsNullOrWhiteSpace(TableName) || string.IsNullOrWhiteSpace(ColumnName)))
			{
				yield return new ValidationResult(
					"Either 'name' (for global option sets) or both 'table' and 'column' (for local option sets) must be provided to update the option.",
					[nameof(Name), nameof(TableName), nameof(ColumnName)]);
			}

			if (!string.IsNullOrWhiteSpace(Name) &&
				(!string.IsNullOrWhiteSpace(TableName) || !string.IsNullOrWhiteSpace(ColumnName)))
			{
				yield return new ValidationResult(
					"'name' cannot be used together with 'table' or 'column'. Please specify only one of them.",
					[nameof(Name), nameof(TableName), nameof(ColumnName)]);
			}

			if ("statecode".Equals(ColumnName, StringComparison.OrdinalIgnoreCase))
			{
				yield return new ValidationResult(
					"StateCode column cannot be changed",
					[nameof(ColumnName)]);
			}




			if (string.IsNullOrWhiteSpace(DisplayName) && string.IsNullOrWhiteSpace(Color))
			{
				yield return new ValidationResult(
					"At least one of 'label' or 'color' must be provided to update the option.",
					[nameof(DisplayName), nameof(Color)]);
			}

			if (!string.IsNullOrWhiteSpace(Color))
			{
				if (!Color.IsValidExadecimalColor())
					yield return new ValidationResult(
						"Color must be a valid hexadecimal color code (e.g., #FF5733, FF5733).",
						[nameof(Color)]);
			}
		}
	}
}
