using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Workflow
{
	[Command("workflow", "run", "get", HelpText = "Get details of a specific workflow run including action outputs.")]
	public class WorkflowRunGetCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Workflow run ID.")]
		public string RunId { get; set; } = "";

		[Option("workflow-id", Order = 2, HelpText = "Workflow definition ID (if run ID is not known).")]
		public string? WorkflowId { get; set; }

		[Option("actions", Order = 3, HelpText = "Include action output details.")]
		public bool IncludeActions { get; set; }

		[Option("format", "f", Order = 4, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
