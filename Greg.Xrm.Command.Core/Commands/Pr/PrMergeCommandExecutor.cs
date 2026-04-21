using Greg.Xrm.Command.Services.Output;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pr
{
	public class PrMergeCommandExecutor(
		IOutput output) : ICommandExecutor<PrMergeCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PrMergeCommand command, CancellationToken cancellationToken)
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

				var pr = await client.PullRequest.Get(owner, name, command.Number);

				if (pr.MergedAt.HasValue)
				{
					output.WriteLine($"PR #{command.Number} is already merged.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}
				if (pr.State == ItemState.Closed)
				{
					output.WriteLine($"PR #{command.Number} is closed.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				if (command.WaitForChecks)
				{
					output.WriteLine("Waiting for CI checks to pass (timeout 30min)...", ConsoleColor.Yellow);
					var checksPassed = await WaitForChecksAsync(client, owner, name, pr.Head.Sha, TimeSpan.FromMinutes(30), cancellationToken);
					if (!checksPassed)
					{
						return CommandResult.Fail("CI checks did not pass within timeout period.");
					}
					output.WriteLine("All checks passed.", ConsoleColor.Green);
				}

				if (command.DryRun)
				{
					output.WriteLine("[DRY RUN] Would merge:", ConsoleColor.Yellow);
					output.WriteLine($"  PR #{command.Number}: {pr.Title}");
					output.WriteLine($"  Method: {command.Method}");
					output.WriteLine($"  Delete branch: {command.DeleteBranch}");
					output.WriteLine($"  Mergeable: {pr.Mergeable?.ToString() ?? "unknown"}");
					return CommandResult.Success();
				}

				output.Write($"Merging PR #{command.Number} ({command.Method})...");

				var mergeResult = command.Method switch
				{
					PrMergeCommand.MergeMethod.Squash => await client.PullRequest.Merge(owner, name, command.Number, new MergePullRequest { MergeMethod = PullRequestMergeMethod.Squash }),
					PrMergeCommand.MergeMethod.Rebase => await client.PullRequest.Merge(owner, name, command.Number, new MergePullRequest { MergeMethod = PullRequestMergeMethod.Rebase }),
					_ => await client.PullRequest.Merge(owner, name, command.Number, new MergePullRequest { MergeMethod = PullRequestMergeMethod.Merge }),
				};

				if (mergeResult.Merged)
				{
					output.WriteLine($" Done ({mergeResult.Message})", ConsoleColor.Green);

					if (command.DeleteBranch && !string.IsNullOrEmpty(pr.Head?.Ref))
					{
						output.Write($"Deleting branch {pr.Head.Ref}...");
						try
						{
							await client.Git.Reference.Delete(owner, name, $"heads/{pr.Head.Ref}");
							output.WriteLine(" Done", ConsoleColor.Green);
						}
						catch
						{
							output.WriteLine(" Failed (branch may already be deleted)", ConsoleColor.Yellow);
						}
					}
				}
				else
				{
					output.WriteLine($" Failed: {mergeResult.Message}", ConsoleColor.Red);
					return CommandResult.Fail($"Merge failed: {mergeResult.Message}");
				}

				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is InvalidOperationException or NotFoundException or ApiValidationException)
			{
				return CommandResult.Fail($"Failed to merge PR: {ex.Message}", ex);
			}
		}

		private async Task<bool> WaitForChecksAsync(GitHubClient client, string owner, string name, string sha, TimeSpan timeout, CancellationToken ct)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (stopwatch.Elapsed < timeout && !ct.IsCancellationRequested)
			{
				var checks = await client.Check.Run.GetAllForReference(owner, name, sha);
				var total = checks.TotalCount;
				if (total == 0) return true; // No checks configured, assume pass

				var completed = checks.CheckRuns.Count(c => c.Status == CheckStatus.Completed);
				var failed = checks.CheckRuns.Count(c => c.Conclusion == CheckConclusion.Failure);

				if (failed > 0) return false;
				if (completed == total) return true;

				output.Write(".");
				await Task.Delay(TimeSpan.FromSeconds(30), ct);
			}
			output.WriteLine();
			return false;
		}

		private static string? ResolveRepo(string? repoArg)
		{
			if (!string.IsNullOrWhiteSpace(repoArg)) return repoArg!.Trim();
			return GitRepositoryResolver.ResolveFromRemote();
		}
	}
}
