using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "delete", HelpText = "Removes a value from an option set (global or local) or from a StatusCode field.")]
	public class DeleteCommand : IValidatableObject
	{
		[Option("name", "n", HelpText = "To be specified only if you want to update a global option set. It's the schema name of the global option set.")]
		public string Name { get; set; } = string.Empty;

		[Option("table", "t", HelpText = "To be specified only if you want to update a local option set. It's the schema name of the table that contains the option set.")]
		public string TableName { get; set; } = string.Empty;

		[Option("column", "c", HelpText = "To be specified only if you want to update a local option set. It's the schema name of the column that contains the option set.")]
		public string ColumnName { get; set; } = string.Empty;


		[Option("value", "v", HelpText = "The value of the option to remove")]
		[Required]
		public int Value { get; set; }



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
		}
	}
}
