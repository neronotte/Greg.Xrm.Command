using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[Command("ribbon", "getdiff", HelpText = "Exports the editable RibbonDiffXml for an application or table ribbon.")]
	public class GetRibbonDiffCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 10, HelpText = "Table logical name. Omit for the application ribbon.")]
		public string TableName { get; set; } = string.Empty;

		[Option("output", "o", Order = 20, HelpText = "File path for the RibbonDiffXml. If omitted, writes only the XML to the console.")]
		public string FileName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 50, HelpText = "Small unmanaged solution containing the target table or Application Ribbons. It is exported to read the RibbonDiffXml. Defaults to the current solution; maximum five tables.")]
		public string? SolutionName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to retrieve the editable ribbon customizations for a table or the application ribbon. Export the RibbonDiffXml, edit the file, then apply it with `ribbon setdiff`. Use `ribbon get` to inspect the expanded ribbon, which cannot be imported as a diff.");
			writer.WriteCodeBlock("pacx ribbon getdiff --table account --solution RibbonDiff --output account.RibbonDiffXml.xml\npacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml", "Powershell");
			writer.WriteParagraph("Use a dedicated solution containing only the target table. When adding it, clear Include entity metadata and Add all assets. For an application ribbon, add Application Ribbons instead. Solutions with more than five tables are rejected. This is the same check and export that setdiff uses. If --solution is omitted, the current default solution is used. Progress and the default solution are reported only with --output; without it, the console receives just the XML.");
			writer.WriteParagraph("When redirecting XML to a file or another program, pass `--nologo` to suppress the PACX startup banner. Use `--output` to save the XML directly while retaining progress messages.");
		}
	}
}
