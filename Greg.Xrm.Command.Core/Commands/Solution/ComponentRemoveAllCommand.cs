using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "removeAll", HelpText = "Removes all solution components from an unmanaged solution.")]
	public class ComponentRemoveAllCommand
	{
		[Option("componentType", "t", Order = 1, HelpText = "Specifies the type of the component to remove from the solution. You can specify the solution component type name (e.g. Entity, Site) or number (e.g. 1, 10403)")]
		[Required]
		public ComponentType ComponentType { get; set; }

		[Option("solution", "s", Order = 2, HelpText = "The unique name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionUniqueName { get; set; }
	}
}
