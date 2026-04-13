using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Workflow
{
	[Command("workflow", "run", "resubmit", HelpText = "Resubmit a failed or cancelled workflow run.")]
	public class WorkflowRunResubmitCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Workflow run ID to resubmit.")]
		public string RunId { get; set; } = "";

		[Option("wait", Order = 2, HelpText = "Wait for the resubmitted run to complete.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
