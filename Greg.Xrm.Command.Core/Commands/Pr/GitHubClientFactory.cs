using Octokit;
using System;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pr
{
	/// <summary>
	/// Factory for creating authenticated GitHub clients.
	/// Reads token from GITHUB_TOKEN environment variable or accepts explicit token.
	/// </summary>
	public static class GitHubClientFactory
	{
		public static GitHubClient Create(string? token = null, string? productHeader = null)
		{
			var resolvedToken = token ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
			if (string.IsNullOrWhiteSpace(resolvedToken))
			{
				throw new InvalidOperationException(
					"GitHub token not provided. Set the GITHUB_TOKEN environment variable or pass --token.");
			}

			var credentials = new Credentials(resolvedToken);
			var client = new GitHubClient(
				new ProductHeaderValue(productHeader ?? "pacx-cli"));
			client.Credentials = credentials;
			return client;
		}
	}
}
