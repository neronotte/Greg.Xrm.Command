using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "get", HelpText = "Print or save the XML of a main form")]
	[Alias("form", "get")]
	public class GetCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table containing the form")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("form", "f", Order = 2, HelpText = "The form name; required when the table has multiple main forms")]
		public string FormName { get; set; } = string.Empty;

		[Option("output", "o", Order = 3, HelpText = "Write the form XML to this file instead of standard output. The folder must exist.")]
		public string? OutputFile { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx forms get --table account --output account-form.xml", "Powershell");
			writer.WriteCodeBlock("pacx forms get -t account -f Information", "Powershell");
		}
	}
}
