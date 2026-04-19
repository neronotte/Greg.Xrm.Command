using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "remove", HelpText = "Removes a solution component from an unmanaged solution.")]
	public class ComponentRemoveCommand
	{
		[Option("componentId", "id", Order = 1, HelpText = "The unique identifier of the solution component to remove.")]
		[Required]
		public Guid ComponentId { get; set; } = Guid.Empty;

		[Option("solution", "s", Order = 2, HelpText = "The unique name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionUniqueName { get; set; }
	}
}
