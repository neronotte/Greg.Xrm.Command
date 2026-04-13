using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Workflow
{
	[Command("workflow", "set-state", HelpText = "Activate or deactivate a workflow definition.")]
	public class WorkflowSetStateCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Workflow definition ID.")]
		public string WorkflowId { get; set; } = "";

		[Option("state", "s", Order = 2, Required = true, HelpText = "Target state: activated, deactivated.")]
		public string State { get; set; } = "";

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
