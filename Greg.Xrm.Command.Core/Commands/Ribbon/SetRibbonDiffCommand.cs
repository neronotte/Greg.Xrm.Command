using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[Command("ribbon", "setdiff", HelpText = "Replaces the application or table RibbonDiffXml from a file.")]
	public class SetRibbonDiffCommand : ICanProvideUsageExample
	{
		[Option("file", "f", Order = 1, HelpText = "Path to an XML file whose root is RibbonDiffXml.")]
		[Required]
		public string FileName { get; set; } = string.Empty;

		[Option("table", "t", Order = 2, HelpText = "Table logical name. Omit for the application ribbon.")]
		public string TableName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 3, HelpText = "Unmanaged solution with at most five segmented tables and optionally Application Ribbons; no other components. Exported and reimported. Defaults to the current solution.")]
		public string? SolutionName { get; set; }

		[Option("backup", "b", Order = 4, HelpText = "File path for a backup of the target ribbon's original RibbonDiffXml. The file must not already exist. Suppresses confirmation.")]
		public string? BackupFile { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Replace the table ribbon customizations from a RibbonDiffXml file:");
			writer.WriteCodeBlock("pacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml", "Powershell");
			writer.WriteParagraph("Save a backup and apply without a confirmation prompt:");
			writer.WriteCodeBlock("pacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml --backup account.RibbonDiffXml.backup.xml", "Powershell");
			writer.WriteParagraph("The command shows the diff, replaces the current unmanaged RibbonDiffXml, and publishes the ribbon. Use a file from ribbon getdiff, not the expanded XML produced by ribbon get.");
			writer.WriteParagraph("Before export, the command checks the selected unmanaged solution in Dataverse. It allows at most five tables added without subcomponents or as shells, plus Application Ribbons, and rejects every other component. The solution is then exported, edited, reimported, and published; no temporary solution is created. If --solution is omitted, the current default solution is used and reported.");
		}
	}
}
