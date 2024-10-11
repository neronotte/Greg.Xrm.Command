using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Forms
{
    [Command("forms", "clean", HelpText = "\"Fixes\" the content of the main form of a given table")]
	[Alias("form", "clean")]
	public class CleanCommand : ICanProvideUsageExample
	{
		[Option("table", "t", HelpText = "The name of the table to which the form belongs")]
		[Required]
		public string TableName { get; set; } = string.Empty;


		[Option("form", "f", HelpText = "The name of the form to initialize. It is required only if the table has more than one Main form.")]
		public string FormName { get; set; } = string.Empty;


		[Option("solution", "s", HelpText = "The name of the solution that contains the table. If not provided, the default solution will be used")]
		public string? SolutionName { get; set; }


		[Option("output", "out", HelpText = "If specified, the command will export the original and updated version of the temporary solution in the specified folder. The folder must exist.")]
		public string? TempDir { get; set; }


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("When you create a new table, the default form comes out with:");
			writer.WriteList(
				"A single tab that has no name",
				"A single section, in the main tab, that has no name",
				"The owner field placed directly in the single section",
				"\"Created On\", \"Created By\", \"Modified On\", \"Modified By\" fields that are invisible");

			writer.WriteParagraph("The current command applies the following changes to the table structure:");

			writer.WriteList(
				"It sets the name attribute on each unnamed tab, starting from the tab label",
				"It sets the name attribute on each unnamed section, starting from the parent tab name and the section label",
				"Removes the \"Owner\" field from the first tab, if present.",
				"Creates a new tab called \"Administration\", with a single untitled 2-columns section, containing \"Created On\", \"Created By\", \"Modified On\", \"Modified By\" and \"Owner\" fields.");

			writer.WriteParagraph("The tab and section names are set only if the tab/section is currently unnamed, and only if the calculated name does not conflicts with a name of another tab/section of the same form.");
			writer.WriteParagraph("The \"Administration\" tab is created only if the form does not contains a tab called tab_admin.");

			writer.WriteParagraph("Please note: to update the form the command needs to create a temporary solution in the target environment. The temporary solution is deleted automatically once the operation is complete.");
		}
	}
}
