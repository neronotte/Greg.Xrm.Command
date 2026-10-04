using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "layout", HelpText = "Print the hierarchy and default visibility of a main form")]
	[Alias("form", "layout")]
	public class LayoutCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table containing the form")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("form", "f", Order = 2, HelpText = "The form name; required when the table has multiple main forms")]
		public string FormName { get; set; } = string.Empty;

		[Option("display", "d", Order = 3, HelpText = "Print labels, names, or both", DefaultValue = LayoutDisplay.Both)]
		public LayoutDisplay Display { get; set; } = LayoutDisplay.Both;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx forms layout --table account --display both", "Powershell");
			writer.WriteCodeBlock("pacx forms layout -t account -f Information -d names", "Powershell");
			writer.WriteParagraph("The tree follows the form designer: header, tabs, sections, fields, and footer. Columns, rows, and cells appear as one-based positions beside their children. Explicit order metadata is printed when present. Visibility includes hidden parents.");
		}
	}

	public enum LayoutDisplay
	{
		Labels,
		Names,
		Both
	}
}
