using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "activate", HelpText = "Activates one or more workflows (Power Automate Flow)")]
	[Alias("flow", "activate")]
	[Alias("flow", "start")]
	public class ActivateCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("id", "id", HelpText = "The ID of the workflow to activate")]
		public Guid WorkflowId { get; set; } = Guid.Empty;

		[Option("name", "n", HelpText = "The unique name of the workflow to activate")]
		public string WorkflowName { get; set; } = string.Empty;

		[Option("solution", "s", HelpText = "The solution that contains the workflows to activate. If not provided, the default solution is used.")]
		public string SolutionName { get; set; } = string.Empty;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!string.IsNullOrWhiteSpace(WorkflowName) && !string.IsNullOrWhiteSpace(SolutionName) && WorkflowId != Guid.Empty)
			{
				yield return new ValidationResult("You cannot specify both --id, --name and --solution options at the same time.", [nameof(WorkflowName), nameof(WorkflowId), nameof(SolutionName)]);
				yield break;
			}

			if (!string.IsNullOrWhiteSpace(WorkflowName) && WorkflowId != Guid.Empty)
			{
				yield return new ValidationResult("You cannot specify both --id and --name options at the same time.", [nameof(WorkflowName), nameof(WorkflowId)]);
			}

			if (!string.IsNullOrWhiteSpace(WorkflowName) && !string.IsNullOrWhiteSpace(SolutionName))
			{
				yield return new ValidationResult("You cannot specify both --name and --solution options at the same time.", [nameof(WorkflowName), nameof(SolutionName)]);
			}

			if (!string.IsNullOrWhiteSpace(SolutionName) && WorkflowId != Guid.Empty)
			{
				yield return new ValidationResult("You cannot specify both --id and --solution options at the same time.", [nameof(SolutionName), nameof(WorkflowId)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to activate one or more workflows (Power Automate Flows) in the current environment. It's really useful in release pipelines, when Power Automate Flows gets deactivated after solution import.");

			writer.WriteParagraph("You can activate a workflow by specifying its unique identifier using the ")
				.WriteCode("--id")
				.Write(" argument, or by specifying its unique name using the ")
				.WriteCode("--name")
				.Write(" argument.");

			writer.WriteParagraph("If you want to activate all the workflows of a given solution, you can simply specify the solution that contains the workflows to activate using the ")
				.WriteCode("--solution")
				.Write(" argument. If not provided, the default solution is used.");


			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# Activate a workflow by its unique identifier");
			writer.WriteLine("pacx workflow activate --id 3fa85f64-5717-4562-b3fc-2c963f66afa6");
			writer.WriteLine();
			writer.WriteLine("# Activate a workflow by its unique name");
			writer.WriteLine("pacx workflow activate --name My_Unique_Workflow_Name");
			writer.WriteLine();
			writer.WriteLine("# Activate all the workflows in a given solution");
			writer.WriteLine("pacx workflow activate --solution \"My Solution Name\"");
			writer.WriteLine();
			writer.WriteLine("# Activate all the workflows in the current default solution");
			writer.WriteLine("pacx workflow activate");
			writer.WriteCodeBlockEnd();
		}
	}
}
