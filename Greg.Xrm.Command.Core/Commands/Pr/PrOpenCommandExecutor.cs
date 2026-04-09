using Greg.Xrm.Command.Services.Output;
using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pr
{
	public class PrOpenCommandExecutor(
		IOutput output) : ICommandExecutor<PrOpenCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PrOpenCommand command, CancellationToken cancellationToken)
		{
			try
			{
				var repo = ResolveRepo(command.Repo);
				var parts = repo.Split('/');
				if (parts.Length != 2)
				{
					return CommandResult.Fail($"Invalid repo format '{repo}'. Expected 'owner/repo'.");
				}
				var (owner, name) = (parts[0], parts[1]);

				var currentBranch = GetCurrentBranch();
				if (string.IsNullOrEmpty(currentBranch))
				{
					return CommandResult.Fail("Unable to determine current branch. Are you in a git repository?");
				}

				var title = command.Title ?? currentBranch;
				var body = command.Body ?? $"Auto-generated PR from branch `{currentBranch}`.";

				if (command.DryRun)
				{
					output.WriteLine("[DRY RUN] Would create:", ConsoleColor.Yellow);
					output.WriteLine($"  Issue: {title}");
					output.WriteLine($"  PR: {currentBranch} -> {command.BaseBranch}");
					output.WriteLine($"  Repo: {repo}");
					return CommandResult.Success();
				}

				var client = GitHubClientFactory.Create(command.Token);

				output.Write($"Creating issue in {repo}...");
				var newIssue = new NewIssue(title)
				{
					Body = body,
				};
				var issue = await client.Issue.Create(owner, name, newIssue);
				output.WriteLine($" Done (#{issue.Number})", ConsoleColor.Green);

				output.Write($"Creating PR {currentBranch} -> {command.BaseBranch}...");
				var newPr = new NewPullRequest(title, currentBranch, command.BaseBranch)
				{
					Body = body,
				};
				var pr = await client.PullRequest.Create(owner, name, newPr);
				output.WriteLine($" Done (#{pr.Number})", ConsoleColor.Green);

				output.WriteLine();
				output.WriteLine($"Issue: {issue.HtmlUrl}");
				output.WriteLine($"PR: {pr.HtmlUrl}");

				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is InvalidOperationException or ApiValidationException or NotFoundException)
			{
				return CommandResult.Fail($"Failed to open PR: {ex.Message}", ex);
			}
		}

		private static string? ResolveRepo(string? repoArg)
		{
			if (!string.IsNullOrWhiteSpace(repoArg)) return repoArg!.Trim();

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "config --get remote.origin.url",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using var process = Process.Start(psi);
				var url = process?.StandardOutput.ReadToEnd().Trim();
				if (string.IsNullOrEmpty(url)) return null;

				// Handle both SSH and HTTPS URLs
				// git@github.com:owner/repo.git -> owner/repo
				// https://github.com/owner/repo.git -> owner/repo
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

		private static string? GetCurrentBranch()
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "rev-parse --abbrev-ref HEAD",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using var process = Process.Start(psi);
				return process?.StandardOutput.ReadToEnd().Trim();
			}
			catch
			{
				return null;
			}
		}
	}
}
