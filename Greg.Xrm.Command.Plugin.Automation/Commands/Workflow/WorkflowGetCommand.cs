using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Plugin.Automation.Commands.Workflow
{
	[Command("workflow", "get", HelpText = "Downloads the JSON definition of a Power Automate Flow.")]
	public class WorkflowGetCommand
	{
		[Option("id", "id", HelpText = "The ID of the workflow to retrieve.")]
		public Guid? WorkflowId { get; set; }

		[Option("name", "n", HelpText = "The unique name of the workflow to retrieve.")]
		public string? WorkflowName { get; set; }

		[Option("output", "o", HelpText = "The output file path (default: flow_definition.json).", DefaultValue = "flow_definition.json")]
		public string? OutputPath { get; set; } = "flow_definition.json";
	}
}
