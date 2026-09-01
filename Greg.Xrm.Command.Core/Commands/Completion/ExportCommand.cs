using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Completion
{
	[Command("completion", "export",
		HelpText = "Prints the pacx command tree (commands, aliases, options) as JSON. Used by the shell completion scripts, can also be consumed by other tooling.")]
	public class ExportCommand : ICanProvideUsageExample
	{
		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Print the command tree as JSON (--nologo keeps the output free from the title banner):");
			writer.WriteCodeBlock("pacx completion export --nologo", "Powershell");

			writer.WriteParagraph("The output lists every visible command with its verbs, aliases and options, plus the help text of each command group. Hidden commands are not included.");
		}
	}
}
