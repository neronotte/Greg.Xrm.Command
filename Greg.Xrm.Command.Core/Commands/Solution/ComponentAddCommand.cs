using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "add", HelpText = "Adds a solution component to an unmanaged solution.")]
	public class ComponentAddCommand : ICanProvideUsageExample
	{
		[Option("componentType", "t", Order = 1, HelpText = "Specifies the type of the component. You can specify the solution component type name (e.g. Entity, Site) or number (e.g. 1, 10403)")]
		[Required]
		public ComponentType ComponentType { get; set; }

		[Option("componentId", "id", Order = 2, HelpText = "The unique identifier of the solution component to add to the solution.")]
		[Required]
		public Guid ComponentId { get; set; } = Guid.Empty;

		[Option("solution", "s", Order = 3, HelpText = "The unique name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionUniqueName { get; set; }

		[Option("addRequiredComponents", "r", Order = 4, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether other solution components that are required by the solution component should also be added to the unmanaged solution.", DefaultValue = false)]
		public bool AddRequiredComponents { get; set; } = false;

		[Option("includeSubcomponents", "is", Order = 5, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether the subcomponents should be included.", DefaultValue = false)]
		public bool IncludeSubcomponents { get; set; } = false;




		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to add a component to an unmanaged solution. For example:");

			writer.WriteCodeBlock("pacx solution component add --componentType Entity --componentId <entity-id> --solution <solution-name>");

			writer.WriteParagraph("This is the list of the (currently known) component types that can be used with this command:");

			var values = Enum.GetNames(typeof(ComponentType))
				.Select(x => (x, (int)Enum.Parse(typeof(ComponentType), x)))
				.OrderBy(x => x.Item2)
				.ToList();

			writer.WriteTable(
				values,
				rowHeaders: ["Name", "Value"],
				rowData: row => [row.Item1, row.Item2.ToString()]);

			writer.WriteParagraph("However, if a solution component type is not listed in the table above, you can still use this command by specifying the component type value instead of the name. For example, if you want to add a Site component (component type 10403), you can run the following command:");

			writer.WriteCodeBlock("pacx solution component add --componentType 10403 --componentId <site-id> --solution <solution-name>");
		}
	}
}
