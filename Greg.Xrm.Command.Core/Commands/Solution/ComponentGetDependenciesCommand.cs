using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "getDependencies", HelpText = "Retrieves the list of solution components that depend from a given component")]
	[Alias("solution", "component", "getdeps")]
	[Alias("solution", "component", "get-dependencies")]
	public class ComponentGetDependenciesCommand
	{
		[Option("componentId", "id", Order = 2, HelpText = "The GUID of the component to retrieve the dependencies for")]
		[Required]
		public Guid ComponentId { get; set; } = Guid.Empty;

		[Option("type", "t", Order = 1, HelpText = "The type of the component to retrieve the dependencies for")]
		[Required]
		public ComponentType? ComponentType { get; set; }
	}
}
