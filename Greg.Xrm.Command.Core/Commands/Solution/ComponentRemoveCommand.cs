using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "remove", HelpText = "Removes a solution component from an unmanaged solution.")]
	public class ComponentRemoveCommand : ICanProvideUsageExample
	{
		[Option("componentId", "id", Order = 1, HelpText = "The ObjectId of the solution component to remove. This is the 'Object ID' value shown by the 'solution component list' command, which corresponds to the objectid field of the solutioncomponent record (NOT the solutioncomponentid).")]
		[Required]
		public Guid ComponentId { get; set; } = Guid.Empty;

		[Option("solution", "s", Order = 2, HelpText = "The unique name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionUniqueName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to remove a component from an unmanaged solution. The `--componentId` parameter expects the **ObjectId** of the component (i.e., the id of the underlying object such as a table, field, or web resource), **not** the `solutioncomponentid`.");
			writer.WriteParagraph("You can retrieve the ObjectId of the components in a solution by running:");
			writer.WriteCodeBlock("pacx solution component list --solution <solution-name>");
			writer.WriteParagraph("The value in the **Object ID** column is what should be passed as `--componentId`. For example:");
			writer.WriteCodeBlock("pacx solution component remove --componentId <object-id> --solution <solution-name>");
		}
	}
}
