using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Completion
{
	[Command("completion", "powershell",
		HelpText = "Prints a PowerShell script that enables tab-completion for pacx commands and options.")]
	public class PowerShellCommand : ICanProvideUsageExample
	{
		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("To enable tab-completion in every session, add this line to your PowerShell profile (`notepad $PROFILE`):");
			writer.WriteCodeBlock("pacx completion powershell --nologo | Out-String | Invoke-Expression", "Powershell");

			writer.WriteParagraph("If you prefer a faster shell startup, save the script once and dot-source it from your profile instead:");
			writer.WriteCodeBlock(@"pacx completion powershell --nologo | Out-File -Encoding utf8 ""$HOME\pacx-completion.ps1""
# then add to your profile:
. ""$HOME\pacx-completion.ps1""", "Powershell");

			writer.WriteParagraph("The script reads the command tree from `pacx completion export` the first time you press TAB, so it always matches the pacx version you have installed. No need to regenerate it after an update.");

			writer.WriteParagraph("Once loaded, you get suggestions for verbs, options and enum values:");
			writer.WriteCodeBlock(@"pacx sol<TAB>                 # completes to: pacx solution
pacx solution <TAB>           # suggests: list, create, delete, ...
pacx plugin trace list <TAB>  # suggests the available options
pacx solution list -f <TAB>   # suggests: Table, TableCompact, Json", "Powershell");

			writer.WriteParagraph("Note for Windows PowerShell 5.1: a bare `-` or `--` does not trigger completion (engine limitation, native argument completers are not invoked for that token). Press TAB right after the command name instead, or type at least one letter of the option name.");
		}
	}
}
