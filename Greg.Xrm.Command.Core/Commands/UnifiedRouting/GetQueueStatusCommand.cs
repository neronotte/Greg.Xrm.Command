using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[Command("unifiedrouting", "queuestatus", HelpText = "List the agents in the queue provided. It uses the Dataverse environment selected using `pacx auth select`")]
    [Alias("ur","queuestatus")]
    public class GetQueueStatusCommand : ICanProvideUsageExample
    {
        [Option("queue", "q")]
        [Required]
        public string? Queue { get; set; }

        [Option("dateTime", "t")]
        public string? DateTimeStatus { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to list all of the agents in a queue. You can simply type");
            writer.WriteCodeBlock("pacx unifiedrouting queuestatus --queue \"QUEUENAME\"", "Command");
            writer.Write("Optionally, you can specify a date in order to list agents status at that time. Example: ");
            writer.WriteCodeBlock("pacx unifiedrouting queuestatus --queue \"QUEUENAME\" --dateTime \"\"", "Command");
            writer.WriteLine();

		}
	}
}
