using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public enum SearchLevel
	{
		Package,
		Assembly,
		Type,
		Step
	}

	[Command("plugin", "list", HelpText = "Searches plugin registrations by name and displays results as a hierarchy tree (package → assembly → type → step → image).")]
	[Alias("plugin", "search")]
	public class ListCommand : ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "Search term. Use a trailing * for a starts-with search (e.g. 'Contoso*'); otherwise a contains search is used.")]
		[Required]
		public string Name { get; set; } = string.Empty;

		[Option("level", "l", Order = 2, HelpText = "Restrict the search to a single hierarchy level: Package, Assembly, Type, or Step. When omitted, all levels are searched. Images are always shown as children of matching steps.")]
		public SearchLevel? Level { get; set; }

		[Option("solution", "s", Order = 3, HelpText = "Unique name of a solution. When provided, only plugin registrations that belong to the specified solution are shown.")]
		public string? SolutionName { get; set; }


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Search for anything containing 'Contoso' across all levels:");
			writer.WriteCodeBlock("pacx plugin list --name Contoso", "Powershell");
			writer.WriteLine();

			writer.WriteParagraph("Search for assemblies whose name starts with 'Contoso' (faster, avoids full-scan on packages):");
			writer.WriteCodeBlock("pacx plugin list --name Contoso* --level Assembly", "Powershell");
			writer.WriteLine();

			writer.WriteParagraph("Search for plugin types containing 'AccountPlugin' in a specific solution:");
			writer.WriteCodeBlock("pacx plugin list --name AccountPlugin --level Type --solution MySolution", "Powershell");
		}
	}
}
