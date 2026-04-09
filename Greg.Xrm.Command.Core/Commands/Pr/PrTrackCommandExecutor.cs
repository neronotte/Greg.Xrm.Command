using Greg.Xrm.Command.Services.Output;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pr
{
	public class PrTrackCommandExecutor(
		IOutput output) : ICommandExecutor<PrTrackCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PrTrackCommand command, CancellationToken cancellationToken)
		{
			try
			{
				var repo = ResolveRepo(command.Repo);
				if (string.IsNullOrEmpty(repo))
				{
					return CommandResult.Fail("Unable to determine repository. Specify with --repo or configure git remote.");
				}
				var parts = repo!.Split('/');
				var (owner, name) = (parts[0], parts[1]);

				var client = GitHubClientFactory.Create(command.Token);

				if (command.Watch)
				{
					return await WatchPrAsync(client, owner, name, command.Number, command.Format, cancellationToken);
				}

				return await DisplayPrStatusAsync(client, owner, name, command.Number, command.Format, cancellationToken);
			}
			catch (Exception ex) when (ex is InvalidOperationException or NotFoundException)
			{
				return CommandResult.Fail($"Failed to track PR: {ex.Message}", ex);
			}
		}

		private async Task<CommandResult> DisplayPrStatusAsync(GitHubClient client, string owner, string name, int number, PrTrackCommand.OutputFormat format, CancellationToken ct)
		{
			var pr = await client.PullRequest.Get(owner, name, number);

			var checks = await client.Check.Run.GetAllForReference(owner, name, pr.Head.Sha);
			var ciStatus = GetCiStatus(checks);

			var reviews = await client.PullRequest.Review.GetAll(owner, name, number);
			var reviewStatus = GetReviewStatus(reviews);

			if (format == PrTrackCommand.OutputFormat.Json)
			{
				output.WriteLine($"{{\"number\":{number},\"state\":\"{pr.State}\",\"mergeable\":{pr.Mergeable?.ToString().ToLower() ?? "null"},\"ci\":\"{ciStatus}\",\"reviews\":\"{reviewStatus}\",\"url\":\"{pr.HtmlUrl}\"}}");
				return CommandResult.Success();
			}

			output.WriteLine($"PR #{number}: {pr.Title}", ConsoleColor.Cyan);
			output.WriteLine($"  URL: {pr.HtmlUrl}");
			output.WriteLine($"  State: {pr.State}");
			output.WriteLine($"  Branch: {pr.Head.Ref} -> {pr.Base.Ref}");
			output.WriteLine($"  Mergeable: {pr.Mergeable?.ToString() ?? "unknown"}");
			output.WriteLine($"  CI: {ciStatus}");
			output.WriteLine($"  Reviews: {reviewStatus}");

			if (pr.MergedAt.HasValue)
			{
				output.WriteLine($"  Merged: {pr.MergedAt.Value:yyyy-MM-dd HH:mm}");
			}
			if (pr.ClosedAt.HasValue && !pr.MergedAt.HasValue)
			{
				output.WriteLine($"  Closed: {pr.ClosedAt.Value:yyyy-MM-dd HH:mm}");
			}

			return CommandResult.Success();
		}

		private async Task<CommandResult> WatchPrAsync(GitHubClient client, string owner, string name, int number, PrTrackCommand.OutputFormat format, CancellationToken ct)
		{
			output.WriteLine($"Watching PR #{number} in {owner}/{name} (polling every 30s)...", ConsoleColor.Yellow);
			output.WriteLine("Press Ctrl+C to stop.", ConsoleColor.Yellow);
			output.WriteLine();

			while (!ct.IsCancellationRequested)
			{
				try
				{
					var pr = await client.PullRequest.Get(owner, name, number);

					if (pr.MergedAt.HasValue)
					{
						output.WriteLine($"PR #{number} has been MERGED!", ConsoleColor.Green);
						return CommandResult.Success();
					}
					if (pr.State == ItemState.Closed)
					{
						output.WriteLine($"PR #{number} has been CLOSED.", ConsoleColor.Red);
						return CommandResult.Success();
					}

					var checks = await client.Check.Run.GetAllForReference(owner, name, pr.Head.Sha);
					var ciStatus = GetCiStatus(checks);
					var reviews = await client.PullRequest.Review.GetAll(owner, name, number);
					var reviewStatus = GetReviewStatus(reviews);

					output.WriteLine($"[{DateTime.Now:HH:mm:ss}] State: {pr.State} | CI: {ciStatus} | Reviews: {reviewStatus} | Mergeable: {pr.Mergeable?.ToString() ?? "unknown"}");

					if (ciStatus.StartsWith("All") && reviewStatus.Contains("Approved"))
					{
						output.WriteLine("PR is ready to merge!", ConsoleColor.Green);
					}
				}
				catch (Exception ex)
				{
					output.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}", ConsoleColor.Red);
				}

				await Task.Delay(TimeSpan.FromSeconds(30), ct);
			}

			return CommandResult.Success();
		}

		private static string GetCiStatus(CheckRunsResponse checks)
		{
			var total = checks.TotalCount;
			if (total == 0) return "No checks configured";

			var completed = checks.CheckRuns.Count(c => c.Status == CheckStatus.Completed);
			var inProgress = checks.CheckRuns.Count(c => c.Status == CheckStatus.InProgress);
			var queued = checks.CheckRuns.Count(c => c.Status == CheckStatus.Queued);
			var failed = checks.CheckRuns.Count(c => c.Conclusion == CheckConclusion.Failure);
			var success = checks.CheckRuns.Count(c => c.Conclusion == CheckConclusion.Success);

			if (completed == total && failed == 0) return $"All {total} checks passed";
			if (failed > 0) return $"{failed}/{total} checks failed";
			return $"{completed}/{total} completed ({inProgress} running, {queued} queued)";
		}

		private static string GetReviewStatus(System.Collections.Generic.IReadOnlyList<PullRequestReview> reviews)
		{
			var approved = reviews.Count(r => r.State == PullRequestReviewState.Approved);
			var changes = reviews.Count(r => r.State == PullRequestReviewState.ChangesRequested);
			var dismissed = reviews.Count(r => r.State == PullRequestReviewState.Dismissed);

			if (approved > 0 && changes == 0) return $"{approved} approved";
			if (changes > 0) return $"{changes} changes requested";
			if (reviews.Count == 0) return "No reviews";
			return $"{reviews.Count} reviews (no approval)";
		}

		private static string? ResolveRepo(string? repoArg)
		{
			if (!string.IsNullOrWhiteSpace(repoArg)) return repoArg!.Trim();

			try
			{
				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "git",
					Arguments = "config --get remote.origin.url",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using var process = System.Diagnostics.Process.Start(psi);
				var url = process?.StandardOutput.ReadToEnd().Trim();
				if (string.IsNullOrEmpty(url)) return null;

				url = url.Replace(".git", "").TrimEnd('/');
				if (url.StartsWith("git@"))
				{
					var colonIndex = url.IndexOf(':');
					return colonIndex >= 0 ? url.Substring(colonIndex + 1) : null;
				}
				var segments = url.Split('/');
				return segments.Length >= 2 ? $"{segments[segments.Length - 2]}/{segments[segments.Length - 1]}" : null;
			}
			catch
			{
				return null;
			}
		}
	}
}
