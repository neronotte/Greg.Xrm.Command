using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "setEnvImage", HelpText = "Sets the image that will be shown in the top left corner of the title bar. This setting applies for all MDAs of a given environment.")]
	[Alias("webresources", "setLogo")]
	[Alias("webresources", "setOrgImage")]
	[Alias("wr", "setEnvImage")]
	[Alias("wr", "setLogo")]
	[Alias("wr", "setOrgImage")]
	public class SetEnvImageCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name of the web resource to set as the organization image. Must be a .png, .jpg or .gif image up to 200x48px.")]
		[Required]
		public string WebResourceUniqueName { get; set; } = string.Empty;


		[Option("appId", "id", Order = 2, HelpText = "Optional appmodule id. If provided, updates CustomThemeDefinition at app level.")]
		public string? AppId { get; set; }

		[Option("appName", "app", Order = 3, HelpText = "Optional app unique/display name. If provided, updates CustomThemeDefinition at app level.")]
		public string? AppName { get; set; }

		[Option("localThemeFile", "ltf", Order = 4, HelpText = "Optional path to a local theme.xml file to update and/or push.")]
		public string? LocalThemeFile { get; set; }

		[Option("color", "col", Order = 5, HelpText = "Optional base palette color for the theme. If provided, updates CustomThemeDefinition at app level. Is mandatory if a new theme file needs to be created, otherwise the logo won't show up.")]
		public string? BasePaletteColor { get; set; }

		[Option("solution", "s", Order = 10, HelpText = "The solution where to save the theme webresource and setting. If not specified, the default solution is considered.")]
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

			if (!string.IsNullOrWhiteSpace(BasePaletteColor))
			{
				if (!BasePaletteColor.IsValidExadecimalColor())
				{
					yield return new ValidationResult("The --color option must be a valid hexadecimal color.", [nameof(BasePaletteColor)]);
				}
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command updates the modern `CustomThemeDefinition` setting and the related theme webresource logo node [as described here](https://learn.microsoft.com/en-us/power-apps/maker/model-driven-apps/modern-theme-overrides).");

			writer.WriteLine("### Workflow overview");
			writer.WriteLine();
			writer.WriteParagraph("The command performs the following steps:");
			writer.WriteLine("1. Connects to the current Dataverse environment.");
			writer.WriteLine("2. Retrieves and validates the specified logo webresource (must be PNG, JPG, or GIF).");
			writer.WriteLine("3. If `--appId` or `--appName` is provided, resolves the target app context.");
			writer.WriteLine("4. Reads the current `CustomThemeDefinition` setting value (app-level or org-level).");
			writer.WriteLine("5. Creates or updates the theme webresource with the new logo reference.");
			writer.WriteLine("6. Publishes the updated theme webresource.");
			writer.WriteLine();

			writer.WriteLine("### Important: the `--color` parameter");
			writer.WriteLine();
			writer.WriteParagraph("The `--color` parameter specifies the base palette color for the theme header.");
			writer.WriteParagraph("**It is REQUIRED when creating a new theme for the first time.** Without it, the theme XML will be incomplete and the logo will not display in the UI.");
			writer.WriteParagraph("When updating an existing theme, `--color` is optional. If omitted, the existing color is preserved. If provided, it overrides the current value.");
			writer.WriteLine();

			writer.WriteLine("### Scenarios");
			writer.WriteLine();

			writer.WriteLine("#### Scenario 1: First-time setup (no theme exists)");
			writer.WriteParagraph("No `CustomThemeDefinition` setting is configured yet. The command creates a new theme webresource, adds it to the specified solution (or the default solution), and saves the setting.");
			writer.WriteParagraph("**`--color` is mandatory** in this case.");
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --color #0078D4 --solution MySolution")
				.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("#### Scenario 2: First-time setup with a local theme file");
			writer.WriteParagraph("You have a local `theme.xml` file that you want to use as the base. The command reads the local file, updates the logo reference, creates the webresource, and saves both remotely and locally.");
			writer.WriteParagraph("If the local file already contains a `basePaletteColor`, `--color` is optional. Otherwise, it is required.");
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --localThemeFile .\\new_\\themes\\theme.xml --solution MySolution")
				.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("#### Scenario 3: Updating an existing remote theme");
			writer.WriteParagraph("The `CustomThemeDefinition` setting already points to a theme webresource on the server. The command retrieves the existing theme, updates the logo reference, and publishes.");
			writer.WriteParagraph("`--color` is optional and will override the existing color only if provided.");
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png")
				.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("#### Scenario 4: Updating an existing theme and syncing to a local file");
			writer.WriteParagraph("The theme already exists remotely. You want to update the logo and also save the updated theme XML to a local file for version control.");
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --localThemeFile .\\new_\\themes\\theme.xml")
				.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("#### Scenario 5: App-specific theme (instead of environment-level)");
			writer.WriteParagraph("Use `--appId` or `--appName` to set the logo for a specific Model-Driven App rather than the entire environment.");
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --appName SalesHub --color #107C10")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --appId 00000000-0000-0000-0000-000000000000")
				.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("#### Scenario 6: Theme setting exists but webresource is missing");
			writer.WriteParagraph("If the `CustomThemeDefinition` setting points to a webresource that no longer exists (e.g., deleted manually), the command will fail with an error. In this case, you must either recreate the webresource manually or clear the setting and re-run the command as a first-time setup.");
			writer.WriteLine();

			writer.WriteLine("### Summary table");
			writer.WriteLine();
			writer.WriteLine("| Setting exists | Theme WR exists | Local file | `--color` required | Behavior |");
			writer.WriteLine("|----------------|-----------------|------------|-------------------|----------|");
			writer.WriteLine("| No | No | No | **Yes** | Creates new theme and setting |");
			writer.WriteLine("| No | No | Yes | Depends on file | Reads local file, creates WR and setting |");
			writer.WriteLine("| Yes | Yes | No | No | Updates remote theme |");
			writer.WriteLine("| Yes | Yes | Yes | No | Updates remote and saves to local |");
			writer.WriteLine("| Yes | No | - | - | **Error**: WR not found |");
			writer.WriteLine();

			writer.WriteLine("### Additional examples");
			writer.WriteLine();
			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("# First-time setup at environment level with a blue theme")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --color #0078D4 --solution MySolution")
				.WriteLine()
				.WriteLine("# Update existing theme with a new logo (no color change)")
				.WriteLine("pacx webresources setEnvImage -n new_logo_v2.png")
				.WriteLine()
				.WriteLine("# Update existing theme and change color")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --color #E74C3C")
				.WriteLine()
				.WriteLine("# Set logo for a specific app by name")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --appName SalesHub --color #107C10")
				.WriteLine()
				.WriteLine("# Use a local theme file as base")
				.WriteLine("pacx webresources setEnvImage -n new_logo.png --localThemeFile .\\themes\\theme.xml --solution MySolution")
				.WriteCodeBlockEnd();
		}
	}
}
