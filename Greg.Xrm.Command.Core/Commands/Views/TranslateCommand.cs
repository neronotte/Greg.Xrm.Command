using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "translate", HelpText = "Sets localized names and descriptions for an existing system view.")]
	[Alias("views", "translate")]
	public class TranslateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The current name of the system view.")]
		[Required]
		public string? Name { get; set; }

		[Option("table", "t", Order = 2, HelpText = "The logical name of the table containing the view. Required when the view name is not unique.")]
		public string? TableName { get; set; }

		[Option("labels", "l", Order = 3, HelpText = "Localized view names in the format LCID:\"label\".")]
		public string? Labels { get; set; }

		[Option("description", "d", Order = 4, HelpText = "Localized view descriptions in the format LCID:\"label\".")]
		public string? Description { get; set; }

		[Option("no-publish", "np", Order = 50, DefaultValue = false, HelpText = "Skips publishing the metadata changes.")]
		public bool NoPublish { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Labels) && string.IsNullOrWhiteSpace(Description))
			{
				yield return new ValidationResult(
					"At least one of 'labels' or 'description' must be provided.",
					[nameof(Labels), nameof(Description)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set localized labels on a system view:");
			writer.WriteCodeBlock(
				"pacx view translate --name \"Active Accounts\" --table account --labels '1033:\"Active Accounts\", 1040:\"Aziende attive\"'",
				"Powershell");
		}
	}
}
