using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	[Command("usersettings", "list", HelpText = "Lists the current values of all tracked user settings for the specified or currently logged-in user.")]
	public class ListCommand : ICanProvideUsageExample
	{
		[Option("user", "u", Order = 1, HelpText = "Domain name of the user whose settings to read (e.g. DOMAIN\\john.doe). If omitted, the current user's settings are shown.")]
		public string? UserDomainName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("List settings for the currently logged-in user:");
			writer.WriteCodeBlock("pacx usersettings list", "Powershell");

			writer.WriteParagraph("List settings for a specific user (by domain name):");
			writer.WriteCodeBlock("pacx usersettings list --user DOMAIN\\\\john.doe", "Powershell");
		}
	}
}
