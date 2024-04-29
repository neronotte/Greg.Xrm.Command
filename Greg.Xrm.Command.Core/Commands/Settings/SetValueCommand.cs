using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "set", HelpText = "Sets the value of a setting in the current environment")]
	[Alias("settings", "setValue")]
	[Alias("setting", "set")]
	[Alias("setting", "setValue")]
	public class SetValueCommand : ICanProvideUsageExample
	{
		[Option("name", "n", HelpText = "The unique name of the setting to set the value for.")]
		[Required]
		public string? Name { get; set; }

		[Option("value", "v", HelpText = "The value to set for the setting. It should match the setting type. For boolean")]
		[Required]
		public string? Value { get; set; }

		[Option("app", "a", HelpText = "The unique name of the app to set the value for (if setting the value at app level).")]
		public string? AppUniqueName { get; set; }

		[Option("solution", "s", HelpText = "The solution where to save the created setting. If not specified, the default solution is considered.")]
		public string? SolutionName { get; set; }



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to set the value of a setting in the current environment, in both app or environment (organization) level. It is a proxy for [SaveSettingValue action](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/savesettingvalue?view=dataverse-latest).");

			writer.WriteParagraph("To set the value of a setting at environment level:");
			writer.WriteCodeBlockStart("powershell").WriteLine("pacx settings setValue -n MySettingName -v MySettingValue").WriteCodeBlockEnd();

			writer.WriteParagraph("To set the value of a setting at app level:");
			writer.WriteCodeBlockStart("powershell").WriteLine("pacx settings setValue -n MySettingName -v MySettingValue -a MyAppName").WriteCodeBlockEnd();
		}
	}
}
