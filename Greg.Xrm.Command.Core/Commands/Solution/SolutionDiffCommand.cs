using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "diff", HelpText = "Compare two solutions or environments and report component differences.")]
	public class SolutionDiffCommand
	{
		[Option("source", "s", Order = 1, Required = true, HelpText = "Source solution unique name or environment.")]
		public string Source { get; set; } = "";

		[Option("target", "t", Order = 2, Required = true, HelpText = "Target solution unique name or environment.")]
		public string Target { get; set; } = "";

		[Option("type", Order = 3, DefaultValue = "solution", HelpText = "Diff type: solution (compare two solutions) or environment (compare two environments).")]
		public string DiffType { get; set; } = "solution";

		[Option("format", "f", Order = 4, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("component-type", Order = 5, HelpText = "Filter by component type: entity, attribute, relationship, plugin, workflow, webresource, etc.")]
		public string? ComponentType { get; set; }
	}
}
