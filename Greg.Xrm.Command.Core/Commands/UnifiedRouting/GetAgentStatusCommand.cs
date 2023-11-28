using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[Command("unifiedrouting", "agentstatus", HelpText = "Creates a new table in the dataverse environment that has previously been selected via `pacx auth select`")]
    [Alias("ur","status")]
    public class GetAgentStatusCommand : ICanProvideUsageExample
    {
        [Option("agentPrimaryEmail", "a")]
        [Required]
        public string? AgentPrimaryEmail { get; set; }

        [Option("queue", "q")]
        public string? Queue { get; set; }

        [Option("dateTime", "t")]
        public string DateTimeStatus { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to create a new table specifying a minimun set of information. You can simply type");
            writer.WriteCodeBlock("pacx table create --name \"My Table\"", "Command");
            writer.Write("to create a new table named \"My Table\" in the solution set as default via ").WriteCode("pacx solution setDefault").WriteLine(".");
            writer.Write("The table schema name will be generated automatically extrapolating only chars, numbers and underscores from the display name,")
                .Write("setting them lowercase, prefixed with the solution's publisher prefix.")
                .WriteLine();
            writer.Write("In this case, if the publisher prefix is ").WriteCode("greg").Write("), the generated schema name will be ").WriteCode("greg_mytable").WriteLine();
            writer.WriteLine();

		}
	}
}
