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

		[Option("solution", "s", Order = 3, HelpText = "Small solution containing the target table or Application Ribbons. Its publisher is used for the temporary solution. Defaults to the current solution; maximum five tables.")]
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
			writer.WriteParagraph("Use a dedicated solution with only the target table, without entity metadata or other assets; for the application ribbon add Application Ribbons. More than five tables are rejected. If --solution is omitted, the current default solution is used and reported. Pass --solution RibbonDiff to choose a small ribbon solution explicitly.");
		}
	}
}
