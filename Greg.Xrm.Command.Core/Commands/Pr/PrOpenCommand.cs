using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pr
{
	[Command("pr", "open", HelpText = "Open a GitHub issue and create a PR from the current branch.")]
	public class PrOpenCommand
	{
		[Option("title", "t", Order = 1, HelpText = "PR title. Defaults to current branch name.")]
		public string? Title { get; set; }

		[Option("body", "b", Order = 2, HelpText = "PR body/description. Defaults to auto-generated from commits.")]
		public string? Body { get; set; }

		[Option("repo", "r", Order = 3, HelpText = "Repository in owner/repo format. Auto-detected from git remote if not provided.")]
		public string? Repo { get; set; }

		[Option("base", Order = 4, HelpText = "Base branch to merge into. Defaults to master.")]
		public string? BaseBranch { get; set; } = "master";

		[Option("token", Order = 5, HelpText = "GitHub personal access token. Reads from GITHUB_TOKEN env var if not provided.")]
		public string? Token { get; set; }

		[Option("dry-run", Order = 6, HelpText = "Show what would be created without actually creating it.")]
		public bool DryRun { get; set; }
	}
}
