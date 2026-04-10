using System;
using System.Diagnostics;

namespace Greg.Xrm.Command.Commands.Pr
{
	/// <summary>
	/// Utility for resolving git remote repository information.
	/// </summary>
	internal static class GitRepositoryResolver
	{
		/// <summary>
		/// Resolves the repository from the git remote origin URL.
		/// Returns null if unable to determine.
		/// </summary>
		internal static string? ResolveFromRemote()
		{
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

				return ParseRepoFromUrl(url);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Parses a git remote URL and returns the owner/repo format.
		/// Supports both SSH (git@github.com:owner/repo.git) and HTTPS (https://github.com/owner/repo.git) URLs.
		/// </summary>
		internal static string? ParseRepoFromUrl(string url)
		{
			url = url.Replace(".git", "").TrimEnd('/');

			// SSH URL: git@github.com:owner/repo
			if (url.StartsWith("git@"))
			{
				var colonIndex = url.IndexOf(':');
				return colonIndex >= 0 ? url.Substring(colonIndex + 1) : null;
			}

			// HTTPS URL: https://github.com/owner/repo
			var segments = url.Split('/');
			return segments.Length >= 2 ? $"{segments[segments.Length - 2]}/{segments[segments.Length - 1]}" : null;
		}
	}
}
