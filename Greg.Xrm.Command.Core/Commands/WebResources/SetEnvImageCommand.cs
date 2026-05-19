using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "setEnvImage", HelpText = "Sets the app/environment logo by updating the modern CustomThemeDefinition setting and theme webresource.")]
	[Alias("webresources", "setLogo")]
	[Alias("webresources", "setOrgImage")]
	[Alias("wr", "setEnvImage")]
	[Alias("wr", "setLogo")]
	[Alias("wr", "setOrgImage")]
	public class SetEnvImageCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name of the image webresource to use as logo. Must be a .png, .jpg or .gif image up to 200x48px (best 156x48px).")]
		[Required]
		public string WebResourceUniqueName { get; set; } = string.Empty;


		[Option("appId", "id", Order = 2, HelpText = "Optional appmodule id. If provided, updates CustomThemeDefinition at app level.")]
		public string? AppId { get; set; }

		[Option("appName", "app", Order = 3, HelpText = "Optional app unique/display name. If provided, updates CustomThemeDefinition at app level.")]
		public string? AppName { get; set; }

		[Option("localThemeFile", "ltf", Order = 4, HelpText = "Optional path to a local theme.xml file to update and/or push.")]
		public string? LocalThemeFile { get; set; }

		[Option("solution", "s", Order = 5, HelpText = "The solution where to save the theme webresource and setting. If not specified, the default solution is considered.")]
		public string? SolutionName { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppName))
			{
				yield return new ValidationResult("Cannot specify both --appId and --appName. Please provide only one.", [nameof(AppId), nameof(AppName)]);
			}

			if (!string.IsNullOrWhiteSpace(AppId) && !Guid.TryParse(AppId, out _))
			{
				yield return new ValidationResult("The --appId option must be a valid GUID.", [nameof(AppId)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command updates the modern `CustomThemeDefinition` setting and the related theme webresource logo node [as described here](https://learn.microsoft.com/en-us/power-apps/maker/model-driven-apps/modern-theme-overrides).");
			writer.WriteParagraph("If `--appId` or `--appName` is provided, the setting is updated at app level; otherwise at environment level.");
			writer.WriteParagraph("If `--localThemeFile` is provided, the command updates the theme definition with the specification of the logo.");

			writer.WriteLine("### Usage examples");

			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --appName SalesHub")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --appId 00000000-0000-0000-0000-000000000000")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --localThemeFile .\\new_\\themes\\theme.xml")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --solution MySolution")
				.WriteCodeBlockEnd();
		}
	}
}
