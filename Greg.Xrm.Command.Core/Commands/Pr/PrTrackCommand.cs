using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pr
{
	[Command("pr", "track", HelpText = "Track the status of a GitHub PR — check CI, reviews, mergeability.")]
	public class PrTrackCommand
	{
		[Option("number", "n", Order = 1, Required = true, HelpText = "PR number to track.")]
		public int Number { get; set; }

		[Option("repo", "r", Order = 2, HelpText = "Repository in owner/repo format. Auto-detected from git remote if not provided.")]
		public string? Repo { get; set; }

		[Option("token", Order = 3, HelpText = "GitHub personal access token. Reads from GITHUB_TOKEN env var if not provided.")]
		public string? Token { get; set; }

		[Option("watch", "w", Order = 4, HelpText = "Continuously watch PR status until merged or closed. Polls every 30s.")]
		public bool Watch { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public OutputFormat Format { get; set; } = OutputFormat.Table;

		public enum OutputFormat { Table, Json }
	}
}
