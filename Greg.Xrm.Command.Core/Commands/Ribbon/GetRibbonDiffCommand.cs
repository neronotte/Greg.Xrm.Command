using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[Command("ribbon", "getdiff", HelpText = "Exports the editable RibbonDiffXml for an application or table ribbon.")]
	public class GetRibbonDiffCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "Table logical name. Omit for the application ribbon.")]
		public string TableName { get; set; } = string.Empty;

		[Option("output", "o", Order = 2, HelpText = "File path for the RibbonDiffXml. If omitted, writes XML to the console.")]
		public string FileName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 3, HelpText = "Small solution containing the target table or Application Ribbons. Its publisher is used for the temporary export solution. Defaults to the current solution; maximum five tables.")]
		public string? SolutionName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Export editable table ribbon changes and import them later:");
			writer.WriteCodeBlock("pacx ribbon getdiff --table account --solution RibbonDiff --output account.RibbonDiffXml.xml\npacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml", "Powershell");
			writer.WriteParagraph("Use a dedicated solution containing only the target table. When adding it, clear Include entity metadata and Add all assets. For an application ribbon, add Application Ribbons instead. Solutions with more than five tables are rejected. If --solution is omitted, the current default solution is used and reported.");
			writer.WriteParagraph("The existing ribbon get command returns the expanded ribbon definition for inspection. It cannot be passed to setdiff.");
		}
	}
}
