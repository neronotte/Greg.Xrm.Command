using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Plugin.Trace
{
	[Command("plugin", "trace", "list",
		HelpText = "Lists the most recent plugin trace log records, newest first.")]
	public class ListCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1,
			HelpText = "Case-insensitive substring match against the plugin type name.")]
		public string? TypeName { get; set; }

		[Option("top", "t", Order = 2,
			HelpText = "Number of trace log records to show (newest first).",
			DefaultValue = 10)]
		public int Top { get; set; } = 10;

		[Option("errors-only", "e", Order = 3,
			HelpText = "Shows only trace log records that contain exception details.",
			DefaultValue = false)]
		public bool ErrorsOnly { get; set; } = false;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Top <= 0 || Top > 1000)
			{
				yield return new ValidationResult("The --top argument must be between 1 and 1000.", [nameof(Top)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Show the 10 most recent plugin trace log records:");
			writer.WriteCodeBlock("pacx plugin trace list", "Powershell");

			writer.WriteParagraph("Show the most recent traces of a given plugin (substring match on the type name):");
			writer.WriteCodeBlock("pacx plugin trace list --name MyPlugin", "Powershell");

			writer.WriteParagraph("Show only failed executions (records containing exception details):");
			writer.WriteCodeBlock("pacx plugin trace list --errors-only", "Powershell");

			writer.WriteParagraph("Typical debug loop: push a new plugin version, trigger it, then check its traces:");
			writer.WriteCodeBlock("pacx plugin trace list -n MyPlugin -e -t 5", "Powershell");

			writer.WriteParagraph("Note: trace log records are written only when trace logging is enabled on the environment (organization setting `plugintracelogsetting`), and Dataverse deletes them automatically after 24 hours.");
		}
	}
}
