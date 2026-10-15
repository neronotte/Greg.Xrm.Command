using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "translate", HelpText = "Sets localized display names for an existing column.")]
	public class TranslateCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table containing the column.")]
		[Required]
		public string? TableName { get; set; }

		[Option("column", "c", Order = 2, HelpText = "The logical name of the column to translate.")]
		[Required]
		public string? ColumnName { get; set; }

		[Option("labels", "l", Order = 3, HelpText = "Localized labels in the format LCID:\"label\", for example 1033:\"Customer\", 1040:\"Cliente\".")]
		[Required]
		public string? Labels { get; set; }

		[Option("no-publish", "np", Order = 50, DefaultValue = false, HelpText = "Skips publishing the metadata changes.")]
		public bool NoPublish { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set one or more localized labels on a column:");
			writer.WriteCodeBlock(
				"pacx column translate --table account --column new_name --labels '1033:\"Customer name\", 1040:\"Nome cliente\"'",
				"Powershell");
		}
	}
}
