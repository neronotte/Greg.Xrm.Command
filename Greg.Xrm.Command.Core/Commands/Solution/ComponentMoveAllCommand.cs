using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "moveAll", HelpText = "Moves a solution component from a solution to another (unmanaged) solution.")]
	public class ComponentMoveAllCommand
	{
		[Option("componentType", "t", Order = 1, HelpText = "Specifies the type of the component to remove from the solution. You can specify the solution component type name (e.g. Entity, Site) or number (e.g. 1, 10403)")]
		[Required]
		public ComponentType ComponentType { get; set; }

		[Option("fromSolution", "from", Order = 3, HelpText = "The unique name of the source solution. If not provided, the default solution will be used.")]
		public string? FromSolutionUniqueName { get; set; }

		[Option("toSolution", "to", Order = 3, HelpText = "The unique name of the target solution. If not provided, the default solution will be used.")]
		public string? ToSolutionUniqueName { get; set; }

		[Option("addRequiredComponents", "r", Order = 4, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether other solution components that are required by the solution component should also be added to the unmanaged solution.", DefaultValue = false)]
		public bool AddRequiredComponents { get; set; } = false;

		[Option("includeSubcomponents", "is", Order = 5, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether the subcomponents should be included.", DefaultValue = false)]
		public bool IncludeSubcomponents { get; set; } = false;
	}
}
