using Greg.Xrm.Command.Services.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Explore
{
	public class ExploreBranchesCommandExecutor : ICommandExecutor<ExploreBranchesCommand>
	{
		private readonly IOutput output;
		private readonly HttpClient httpClient;

		public ExploreBranchesCommandExecutor(IOutput output) : this(output, CreateHttpClient())
		{
		}

		public ExploreBranchesCommandExecutor(IOutput output, HttpClient httpClient)
		{
			this.output = output;
			this.httpClient = httpClient;
		}

		private static HttpClient CreateHttpClient()
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "pacx-explore-branches");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
			return client;
		}

		public async Task<CommandResult> ExecuteAsync(ExploreBranchesCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.WriteLine($"Fetching branches from {command.Owner}/{command.Repo}...", ConsoleColor.Cyan);

				var branches = await GetBranchesAsync(command.Owner, command.Repo, cancellationToken);

				if (branches.Count == 0)
				{
					output.WriteLine("No branches found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteLine($"Found {branches.Count} branches:", ConsoleColor.Green);

				if (command.Format == "json")
				{
					var json = JsonSerializer.Serialize(branches, new JsonSerializerOptions { WriteIndented = true });
					output.WriteLine(json);
				}
				else
				{
					output.WriteTable(branches,
						() => new[] { "Name", "Protected", "LastCommit" },
						b => new[] {
							b.Name,
							b.Protected ? "Yes" : "",
							b.LastCommit?.ToString("yyyy-MM-dd") ?? "-"
						}
					);
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Error fetching branches: {ex.Message}", ex);
			}
		}

		private async Task<List<BranchInfo>> GetBranchesAsync(string owner, string repo, CancellationToken ct)
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/branches";
			var response = await httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync(ct);
			var branches = JsonSerializer.Deserialize<List<GitHubBranch>>(json) ?? new();

			return branches.Select(b => new BranchInfo
			{
				Name = b.Name,
				Protected = b.Protected,
				LastCommit = b.Commit?.Date
			}).ToList();
		}

		private class GitHubBranch
		{
			public string Name { get; set; } = "";
			public bool Protected { get; set; }
			public GitHubCommit? Commit { get; set; }
		}

		private class GitHubCommit
		{
			public string Sha { get; set; } = "";
			public DateTime? Date { get; set; }
		}
	}

	public class BranchInfo
	{
		public string Name { get; set; } = "";
		public bool Protected { get; set; }
		public DateTime? LastCommit { get; set; }
	}

	public class ExploreCompareCommandExecutor : ICommandExecutor<ExploreCompareCommand>
	{
		private readonly IOutput output;
		private readonly HttpClient httpClient;

		public ExploreCompareCommandExecutor(IOutput output) : this(output, CreateHttpClient())
		{
		}

		public ExploreCompareCommandExecutor(IOutput output, HttpClient httpClient)
		{
			this.output = output;
			this.httpClient = httpClient;
		}

		private static HttpClient CreateHttpClient()
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "pacx-explore-compare");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
			return client;
		}

		public async Task<CommandResult> ExecuteAsync(ExploreCompareCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.WriteLine($"Comparing {command.Base}...{command.Head} in {command.Owner}/{command.Repo}...", ConsoleColor.Cyan);

				var comparison = await CompareBranchesAsync(
					command.Owner, command.Repo, command.Base, command.Head, cancellationToken);

				output.WriteLine();
				output.WriteLine($"Base: {command.Base}", ConsoleColor.Gray);
				output.WriteLine($"Head: {command.Head}", ConsoleColor.Gray);
				output.WriteLine($"Ahead: {comparison.AheadBy} commits", ConsoleColor.Green);
				output.WriteLine($"Behind: {comparison.BehindBy} commits", ConsoleColor.Yellow);
				output.WriteLine();

				if (comparison.Commits.Count > 0)
				{
					output.WriteLine($"Commits in {command.Head} not in {command.Base} ({comparison.Commits.Count}):", ConsoleColor.Cyan);

					if (command.Format == "json")
					{
						var json = JsonSerializer.Serialize(comparison.Commits, new JsonSerializerOptions { WriteIndented = true });
						output.WriteLine(json);
					}
					else
					{
						foreach (var commit in comparison.Commits.Take(20))
						{
							var shortSha = commit.Sha.Length > 7 ? commit.Sha[..7] : commit.Sha;
							output.WriteLine($"  {shortSha} - {commit.Message} ({commit.Author})");
						}

						if (comparison.Commits.Count > 20)
						{
							output.WriteLine($"  ... and {comparison.Commits.Count - 20} more commits", ConsoleColor.Gray);
						}
					}
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Error comparing branches: {ex.Message}", ex);
			}
		}

		private async Task<ComparisonResult> CompareBranchesAsync(
			string owner, string repo, string @base, string head, CancellationToken ct)
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/compare/{@base}...{head}";
			var response = await httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync(ct);
			var comparison = JsonSerializer.Deserialize<GitHubComparison>(json) ?? new();

			return new ComparisonResult
			{
				AheadBy = comparison.AheadBy,
				BehindBy = comparison.BehindBy,
				Commits = comparison.Commits?.Select(c => new CommitInfo
				{
					Sha = c.Sha,
					Message = c.Commit?.Message ?? "",
					Author = c.Commit?.Author?.Name ?? ""
				}).ToList() ?? new()
			};
		}

		private class GitHubComparison
		{
			public int AheadBy { get; set; }
			public int BehindBy { get; set; }
			public List<GitHubCommit>? Commits { get; set; }
		}

		private class GitHubCommit
		{
			public string Sha { get; set; } = "";
			public GitHubCommitDetail? Commit { get; set; }
		}

		private class GitHubCommitDetail
		{
			public string Message { get; set; } = "";
			public GitHubAuthor? Author { get; set; }
		}

		private class GitHubAuthor
		{
			public string Name { get; set; } = "";
		}
	}

	public class ComparisonResult
	{
		public int AheadBy { get; set; }
		public int BehindBy { get; set; }
		public List<CommitInfo> Commits { get; set; } = new();
	}

	public class CommitInfo
	{
		public string Sha { get; set; } = "";
		public string Message { get; set; } = "";
		public string Author { get; set; } = "";
	}
}
