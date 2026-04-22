using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "addTable", HelpText = "Adds a table (entity) to an unmanaged solution.")]
	[Alias("solution", "component", "add-table")]
	[Alias("solution", "component", "addEntity")]
	[Alias("solution", "component", "add-entity")]
	public class ComponentAddTableCommand : ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The schema name (or display name) of the table to add to the solution.")]
		[Required]
		public string? TableName { get; set; }

		[Option("solution", "s", Order = 3, HelpText = "The unique name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionUniqueName { get; set; }

		[Option("addRequiredComponents", "r", Order = 4, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether other solution components that are required by the solution component should also be added to the unmanaged solution.", DefaultValue = false)]
		public bool AddRequiredComponents { get; set; } = false;

		[Option("includeSubcomponents", "is", Order = 5, HelpText = "To be specified only if `componentType` is `Entity`. Indicates whether the subcomponents should be included.", DefaultValue = false)]
		public bool IncludeSubcomponents { get; set; } = false;




		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to add a table to an unmanaged solution by name. For example:");

			writer.WriteCodeBlock("pacx solution component addTable -t account --solution <solution-name>");
		}
	}
}
