using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pr
{
	[Command("pr", "merge", HelpText = "Merge a GitHub PR when CI passes and reviews are approved.")]
	public class PrMergeCommand
	{
		[Option("number", "n", Order = 1, Required = true, HelpText = "PR number to merge.")]
		public int Number { get; set; }

		[Option("repo", "r", Order = 2, HelpText = "Repository in owner/repo format. Auto-detected from git remote if not provided.")]
		public string? Repo { get; set; }

		[Option("token", Order = 3, HelpText = "GitHub personal access token. Reads from GITHUB_TOKEN env var if not provided.")]
		public string? Token { get; set; }

		[Option("method", "m", Order = 4, DefaultValue = MergeMethod.Squash, HelpText = "Merge method: squash, merge, rebase.")]
		public MergeMethod Method { get; set; } = MergeMethod.Squash;

		[Option("wait", Order = 5, HelpText = "Wait for CI checks to pass before merging. Polls every 30s, times out after 30min.")]
		public bool WaitForChecks { get; set; }

		[Option("delete-branch", "d", Order = 6, HelpText = "Delete the source branch after merging.")]
		public bool DeleteBranch { get; set; }

		[Option("dry-run", Order = 7, HelpText = "Check mergeability without actually merging.")]
		public bool DryRun { get; set; }

		public enum MergeMethod { Squash, Merge, Rebase }
	}
}
