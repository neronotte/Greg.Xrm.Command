using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Plugin.Automation.Commands.Workflow
{
	[Command("workflow", "run", "list", HelpText = "Lists the recent runs for a specific Flow.")]
	public class WorkflowRunListCommand
	{
		[Option("flow", "f", HelpText = "The unique name or ID of the flow.")]
		[Required]
		public string? FlowIdentifier { get; set; }

		[Option("limit", "l", HelpText = "The maximum number of runs to return (default: 10).", DefaultValue = 10)]
		public int Limit { get; set; } = 10;
	}
}
