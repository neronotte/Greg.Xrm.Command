using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "list", HelpText = "Returns a list of workflows (Power Automate Flow)")]
	[Alias("flow", "list")]
	public class ListCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name (or part of it) of the workflow to retrieve")]
		public string SearchQuery { get; set; } = string.Empty;

		[Option("category", "c", Order = 2, HelpText = "The category of the workflows to return.")]
		public Workflow.Category? Category { get; set; } = null;

		[Option("solution", "s", Order = 3, HelpText = "The solution that contains the workflows to return. If not provided, the default solution is used. Pass * to avoid filtering by solution.")]
		public string SolutionName { get; set; } = string.Empty;

		[Option("live", "l", Order = 4, HelpText = "Compare Dataverse state with the live Power Automate state of cloud flows.")]
		public bool Live { get; set; }

		[Option("fail-on-mismatch", "fm", Order = 5, HelpText = "With --live, exit with code 1 if any cloud flow is MISMATCH or UNKNOWN.")]
		public bool FailOnMismatch { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (FailOnMismatch && !Live)
				yield return new ValidationResult("--fail-on-mismatch requires --live.", [nameof(FailOnMismatch)]);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to return a list of the workflows (Power Automate Flows) in the current environment.");

			writer.WriteParagraph("Calling the command without any argument will return all the workflows in the current default solution.");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow list");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("You can filter the returned workflows by specifying a search query using the ")
				.WriteCode("--name")
				.Write(" argument. This will return all the workflows that contains the specified text in their name.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# List all workflows that contains 'Approval' in their name");
			writer.WriteLine("pacx workflow list --name \"Approval\"");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("You can also filter the returned workflows by specifying the solution that contains the workflows using the ")
				.WriteCode("--solution")
				.Write(" argument. If not provided, the default solution is used. You can pass * to avoid filtering by solution.");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# List all workflows in a given solution");
			writer.WriteLine("pacx workflow list --solution \"My Solution Name\"");
			writer.WriteLine();
			writer.WriteLine("# List all workflows in all solutions");
			writer.WriteLine("pacx workflow list --solution *");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("Finally, you can filter the returned workflows by specifying their category using the ")
				.WriteCode("--category")
				.Write(" argument.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("# List all Power Automate Flows");
			writer.WriteLine("pacx workflow list --category ModernFlow");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("Dataverse can report a cloud flow as Activated even when Power Automate has stopped it. For example, a failed solution import can roll back Dataverse's state without undoing a stop in Power Automate. Use --live (-l) to compare both states for cloud flows (ModernFlow). Other workflow categories show n/a in the live columns.");

			writer.WriteParagraph("In a release pipeline, run this after solution import:");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow list --solution my_solution --category ModernFlow --live --fail-on-mismatch");
			writer.WriteCodeBlockEnd();
			writer.WriteParagraph("With --fail-on-mismatch (-fm), exit code 1 means at least one cloud flow is MISMATCH or UNKNOWN. UNKNOWN includes flows that were not found in the Power Automate listing. Authentication or API errors return 2. Otherwise the command returns 0. --fail-on-mismatch requires --live.");

			writer.WriteParagraph("Live checks use the active PACX authentication profile. Users need the Power Platform Administrator role or the System Administrator role in the environment. Service principals must be registered once as a management app:");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pac admin application register --application-id <id>");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("If Dataverse says Activated but Power Automate says Stopped or Suspended, open the flow's details page in Power Automate. Check the suspension reason (if available) and resolve any connection, ownership, or policy issues. If the flow should be running, turn it on from that page and rerun the live check. If the portal also thinks it is already on, coordinate a controlled turn-off/turn-on with the flow owner, then verify the live state again. This command does not change any flow.");
		}
	}
}
