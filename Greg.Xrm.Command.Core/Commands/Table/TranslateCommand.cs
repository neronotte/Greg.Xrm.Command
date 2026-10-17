using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Translations;

namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "translate", HelpText = "Sets localized singular and plural names for an existing table.")]
	public class TranslateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table to translate.")]
		[Required]
		public string? TableName { get; set; }

		[Option("singular", "s", Order = 2, HelpText = "Localized singular names in the format LCID:\"label\".")]
		[Required]
		public string? Singular { get; set; }

		[Option("plural", "p", Order = 3, HelpText = "Localized plural names in the format LCID:\"label\".")]
		[Required]
		public string? Plural { get; set; }

		[Option("description", "d", Order = 4, HelpText = "Localized table descriptions in the format LCID:\"label\".")]
		public string? Description { get; set; }

		[Option("no-publish", "np", Order = 50, DefaultValue = false, HelpText = "Skips publishing the metadata changes.")]
		public bool NoPublish { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Singular) || string.IsNullOrWhiteSpace(Plural))
				yield break;

			IReadOnlyDictionary<int, string> singular;
			IReadOnlyDictionary<int, string> plural;
			try
			{
				singular = new TranslationParser().Parse(Singular);
				plural = new TranslationParser().Parse(Plural);
			}
			catch (CommandException)
			{
				yield break;
			}

			if (!singular.Keys.OrderBy(x => x).SequenceEqual(plural.Keys.OrderBy(x => x)))
			{
				yield return new ValidationResult(
					"The singular and plural translations must contain the same LCIDs.",
					[nameof(Singular), nameof(Plural)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set localized singular and plural table names:");
			writer.WriteCodeBlock(
				"pacx table translate --table account --singular '1033:\"Account\", 1040:\"Azienda\"' --plural '1033:\"Accounts\", 1040:\"Aziende\"'",
				"Powershell");
		}
	}
}
