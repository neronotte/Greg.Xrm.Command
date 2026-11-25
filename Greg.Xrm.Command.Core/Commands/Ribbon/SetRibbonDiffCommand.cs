using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[Command("ribbon", "setdiff", HelpText = "Replaces the application or table RibbonDiffXml from a file.")]
	public class SetRibbonDiffCommand : ICanProvideUsageExample
	{
		[Option("file", "f", Order = 1, HelpText = "Path to a RibbonDiffXml file. Validated locally against the ribbon schema before connecting.")]
		[Required]
		public string FileName { get; set; } = string.Empty;

		[Option("table", "t", Order = 10, HelpText = "Table logical name. Omit for the application ribbon.")]
		public string TableName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 50, HelpText = "Unmanaged solution with at most five segmented tables and optionally Application Ribbons; no other components. Exported and reimported. Defaults to the current solution.")]
		public string? SolutionName { get; set; }

		[Option("backup", "b", Order = 20, HelpText = "File path for a backup of the target ribbon's original RibbonDiffXml. Overwrites the file if it already exists. Created even if the XML is unchanged. Suppresses confirmation.")]
		public string? BackupFile { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to apply edited ribbon customizations to a table or the application ribbon. Start with a file exported by `ribbon getdiff`, make your changes, then use `--backup` to preserve the current definition before replacing it. The command imports the change and publishes only the selected ribbon scope.");
			writer.WriteCodeBlock("pacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml", "Powershell");
			writer.WriteParagraph("Save a backup and apply without a confirmation prompt (an unchanged ribbon is still backed up):");
			writer.WriteCodeBlock("pacx ribbon setdiff --table account --solution RibbonDiff --file account.RibbonDiffXml.xml --backup account.RibbonDiffXml.backup.xml", "Powershell");
			writer.WriteParagraph("The command shows the diff, replaces the current unmanaged RibbonDiffXml, and publishes only that table or the application ribbon. Use a file from ribbon getdiff, not the expanded XML produced by ribbon get.");
			writer.WriteParagraph("Without --backup, the command requires an interactive terminal and asks for confirmation, defaulting to no. Declining or running non-interactively fails without changing the ribbon. Use --backup for automation.");
			writer.WriteParagraph("Before export, the command checks the selected unmanaged solution in Dataverse. It allows at most five tables added without subcomponents or as shells, plus Application Ribbons, and rejects every other component. The solution is then exported, edited, reimported, and published; no temporary solution is created. If --solution is omitted, the current default solution is used and reported.");
		}
	}
}
