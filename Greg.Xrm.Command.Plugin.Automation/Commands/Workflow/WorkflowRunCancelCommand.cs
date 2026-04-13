using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Workflow
{
	[Command("workflow", "run", "cancel", HelpText = "Cancel a running workflow.")]
	public class WorkflowRunCancelCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Workflow run ID to cancel.")]
		public string RunId { get; set; } = "";

		[Option("force", "f", Order = 2, HelpText = "Force cancellation without confirmation.")]
		public bool Force { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
