using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "list", HelpText = "Returns a list of workflows (Power Automate Flow)")]
	[Alias("flow", "list")]
	public class ListCommand : ICanProvideUsageExample
	{
		[Option("name", "n", HelpText = "The unique name (or part of it) of the workflow to retrieve")]
		public string SearchQuery { get; set; } = string.Empty;

		[Option("category", "c", HelpText = "The category of the workflows to return.")]
		public Workflow.Category? Category { get; set; } = null;

		[Option("solution", "s", HelpText = "The solution that contains the workflows to return. If not provided, the default solution is used. Pass * to avoid filtering by solution.")]
		public string SolutionName { get; set; } = string.Empty;


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
		}
	}
}
