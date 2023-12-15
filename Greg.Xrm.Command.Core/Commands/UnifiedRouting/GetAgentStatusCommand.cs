using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
    [Command("unifiedrouting", "agentStatus", HelpText = "Get the agent status with the primary email/domain name provided. Optionally, you can specify a date in order to get agent status at that time. It uses the Dataverse environment selected using `pacx auth select`")]
    [Alias("ur","status")]
	public class GetAgentStatusCommand : ICanProvideUsageExample
	{
		[Option("agentPrimaryEmail", "a", "Agent primary email (or domain name) used to perform the query.")]
		[Required]
		public string? AgentPrimaryEmail { get; set; }

		[Option("dateTime", "t", "Date and time (local time) used to perform the query. Format dd/MM/yyyy HH:mm.")]
		public string? DateTimeFilter { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
		}
	}
}
