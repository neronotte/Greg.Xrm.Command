using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "setEnvImage", HelpText=  "Sets the image that will be shown in the top left corner of the title bar. This setting applies for all MDAs of a given environment.")]
	[Alias("webresources", "setLogo")]
	[Alias("webresources", "setOrgImage")]
	[Alias("wr", "setEnvImage")]
	[Alias("wr", "setLogo")]
	[Alias("wr", "setOrgImage")]
	public class SetEnvImageCommand : ICanProvideUsageExample
	{
		[Option("name", "n", HelpText = "The unique name of the web resource to set as the organization image. Must be a .png, .jpg or .gif image up to 200x50px.")]
		[Required]
		public string WebResourceUniqueName { get; set; } = string.Empty;


		[Option("cloneTheme", "c", HelpText = "If true, the theme will be cloned and the new image will be set as the organization image.", DefaultValue =false)]
		public bool CloneTheme { get; set; } = false;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("The aim of the current command is to streamline the process of setting the logo that is shown in the of the title bar of each model driven app that is present in a given environment.");
			writer.WriteLine("If you want to change the logo manually, you need to leverage the _old theming_ capability, accessible via the _legacy settings UI_ under _Customizations > Themes_. You need to:");
			writer.WriteLine();

			writer.WriteLine("1. Create a new theme (from scratch or by cloning an existing default theme) --> this is required because you cannot apply a logo on one of the default themes.");
			writer.WriteLine("2. Set the logo on the new theme. --> The logo image should be a webresource already present in the environment.");
			writer.WriteLine("3. Apply the new theme to the environment by publishing it.");
			writer.WriteLine();

			writer.WriteLine("This command will do all these steps for you, in a single command, performing also a few consistency checks. It will:");
			writer.WriteLine();

			writer.WriteLine("1. Check whether the current environment contains a webresource whose name is passed in the `--name` argument.");
			writer.WriteLine("2. If present, checks if it's of the right type.");
			writer.WriteLine("3. Retrieves the current default legacy theme for the environment. ");
			writer.WriteLine("4. If the theme is a default theme, or the `--cloneTheme` argument is explicitly passed, creates a copy of the theme using the same name with suffix ` - Copy`.");
			writer.WriteLine("5. Sets the logo on the theme (the found or the cloned one).");
			writer.WriteLine("6. Publishes the theme.");
			writer.WriteLine();

			writer.WriteLine("You can then hit F5 in your environment to see the applied logo.");

			writer.WriteLine("> **Please note**: The _old theming_ capability is not available in the new **Power Platform Environment Settings** App. To create modern themes you can leverage [_n.ModernThemeBuilder XrmToolbox plugin](https://www.xrmtoolbox.com/plugins/Greg.Xrm.ModernThemeBuilder/), or [do it manually](https://learn.microsoft.com/en-us/power-apps/maker/model-driven-apps/modern-theme-overrides).");
			writer.WriteLine("> However, modern themes does not provides any capability to change the logo, thus leveraging this PACX command is, right now, the quickest viable option to achieve this goal.");

			writer.WriteLine("### Usage examples");

			writer.WriteLine("```");
			writer.WriteLine("pacx webresources setEnvImage -n new_Logo.png");
			writer.WriteLine("pacx webresources setEnvImage -n new_Logo.png -c");
			writer.WriteLine("pacx wr setLogo -n new_Logo.png");
			writer.WriteLine("pacx wr setLogo -n new_Logo.png -c");
			writer.WriteLine("```");
		}
	}
}
