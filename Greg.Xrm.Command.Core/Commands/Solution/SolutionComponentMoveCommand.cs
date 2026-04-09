using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component-move", HelpText = "Move individual components between solutions.")]
	public class SolutionComponentMoveCommand
	{
		[Option("component", "c", Order = 1, Required = true, HelpText = "Component unique name or ID to move.")]
		public string ComponentName { get; set; } = "";

		[Option("type", "t", Order = 2, Required = true, HelpText = "Component type: entity, attribute, relationship, plugin, workflow, webresource, optionset, etc.")]
		public string ComponentType { get; set; } = "";

		[Option("from", Order = 3, Required = true, HelpText = "Source solution unique name.")]
		public string FromSolution { get; set; } = "";

		[Option("to", Order = 4, Required = true, HelpText = "Target solution unique name.")]
		public string ToSolution { get; set; } = "";

		[Option("include-dependencies", "d", Order = 5, HelpText = "Automatically include dependent components.")]
		public bool IncludeDependencies { get; set; }

		[Option("dry-run", Order = 6, HelpText = "Show what would be moved without actually moving.")]
		public bool DryRun { get; set; }
	}
}
