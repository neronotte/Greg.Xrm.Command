using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Explore
{
	[Command("explore", "branches", HelpText = "List all remote branches from upstream repository.")]
	public class ExploreBranchesCommand
	{
		[Option("owner", "o", Order = 1, HelpText = "Repository owner (default: neronotte).")]
		public string Owner { get; set; } = "neronotte";

		[Option("repo", "r", Order = 2, HelpText = "Repository name (default: Greg.Xrm.Command).")]
		public string Repo { get; set; } = "Greg.Xrm.Command";

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("explore", "compare", HelpText = "Compare branches and show commit differences.")]
	public class ExploreCompareCommand
	{
		[Option("owner", "o", Order = 1, HelpText = "Repository owner (default: neronotte).")]
		public string Owner { get; set; } = "neronotte";

		[Option("repo", "r", Order = 2, HelpText = "Repository name (default: Greg.Xrm.Command).")]
		public string Repo { get; set; } = "Greg.Xrm.Command";

		[Option("base", "b", Order = 3, HelpText = "Base branch (default: master).")]
		public string Base { get; set; } = "master";

		[Option("head", "h", Order = 4, HelpText = "Head branch to compare.")]
		[Required]
		public string Head { get; set; } = "";

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
