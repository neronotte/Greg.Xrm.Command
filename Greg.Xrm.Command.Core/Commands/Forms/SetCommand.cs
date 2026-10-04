using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "set", HelpText = "Replace the XML of a main form")]
	[Alias("form", "set")]
	public class SetCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table containing the form")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("form", "f", Order = 2, HelpText = "The form name; required when the table has multiple main forms")]
		public string FormName { get; set; } = string.Empty;

		[Option("file", "i", Order = 3, HelpText = "Path to a file containing the complete <form> XML")]
		[Required]
		public string FileName { get; set; } = string.Empty;

		[Option("backup", "b", Order = 4, HelpText = "Write the current form XML to this file before updating it")]
		public string? BackupFile { get; set; }

		[Option("solution", "s", Order = 5, HelpText = "Solution whose publisher is used for the temporary import; defaults to the current solution")]
		public string? SolutionName { get; set; }

		[Option("fast", "ft", Order = 6, HelpText = "Update the systemform directly instead of using a temporary solution", DefaultValue = false)]
		public bool Fast { get; set; }

		[Option("publish", "p", Order = 7, HelpText = "Publish the table and its forms after updating. Defaults to false.", DefaultValue = false)]
		public bool Publish { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx forms get -t account -o account-form.xml", "Powershell");
			writer.WriteCodeBlock("pacx forms set -t account --file account-form.xml --backup account-form-before.xml --publish true", "Powershell");
			writer.WriteParagraph("The input must contain one complete <form> element. Form XML taken from a solution export that contains solutionaction diff markers is rejected. By default, set imports the replacement through a temporary solution and leaves it unpublished. --publish publishes the table and its associated forms.");
			writer.WriteParagraph("--fast updates formxml directly. Dataverse also stores a formjson representation, which may not be synchronized by a direct formxml update. Before publishing in production, apply the final XML without --fast and with --publish true.");
		}
	}
}
