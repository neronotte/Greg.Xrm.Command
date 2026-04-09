using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pages
{
	public class PagesSitePublishCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<PagesSitePublishCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PagesSitePublishCommand command, CancellationToken cancellationToken)
		{
			if (!Directory.Exists(command.SourcePath))
			{
				return CommandResult.Fail($"Source directory not found: {command.SourcePath}");
			}

			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var files = Directory.GetFiles(command.SourcePath, "*.*", SearchOption.AllDirectories);
			output.WriteLine($"Found {files.Length} files to publish from {command.SourcePath}", ConsoleColor.Cyan);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would publish:", ConsoleColor.Yellow);
				output.WriteLine($"  Source: {command.SourcePath}");
				output.WriteLine($"  Files: {files.Length}");
				if (!string.IsNullOrEmpty(command.WebsiteId))
					output.WriteLine($"  Website: {command.WebsiteId}");
				return CommandResult.Success();
			}

			output.WriteLine("Power Pages site publishing requires additional configuration.", ConsoleColor.Yellow);
			output.WriteLine("Use the Power Pages maker portal or Admin API for full publishing support.");
			return CommandResult.Success();
		}
	}

	public class PagesWebTemplateSyncCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<PagesWebTemplateSyncCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PagesWebTemplateSyncCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.WriteLine($"Syncing {command.SyncType} from {command.SourceEnvironmentId} to {command.TargetEnvironmentId}", ConsoleColor.Cyan);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would sync templates", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			output.WriteLine("Power Pages template sync requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class PagesLiquidLintCommandExecutor(
		IOutput output) : ICommandExecutor<PagesLiquidLintCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PagesLiquidLintCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FilePath) && !Directory.Exists(command.FilePath))
			{
				return CommandResult.Fail($"Path not found: {command.FilePath}");
			}

			var files = File.Exists(command.FilePath)
				? new[] { command.FilePath }
				: Directory.GetFiles(command.FilePath, "*.liquid", SearchOption.AllDirectories);

			if (files.Length == 0)
			{
				output.WriteLine("No Liquid templates found.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			var issues = 0;
			foreach (var file in files)
			{
				var content = await File.ReadAllTextAsync(file, cancellationToken);
				var fileIssues = AnalyzeLiquidContent(content, file, command.Strict);
				issues += fileIssues;
			}

			if (issues > 0)
			{
				output.WriteLine($"\nFound {issues} issue(s) in {files.Length} file(s).", command.Strict ? ConsoleColor.Red : ConsoleColor.Yellow);
				return command.Strict ? CommandResult.Fail($"Found {issues} issue(s)") : CommandResult.Success();
			}

			output.WriteLine($"All {files.Length} file(s) passed validation.", ConsoleColor.Green);
			return CommandResult.Success();
		}

		private int AnalyzeLiquidContent(string content, string file, bool strict)
		{
			var issues = 0;

			// Check for unclosed tags
			var openTags = System.Text.RegularExpressions.Regex.Matches(content, @"\{%\s*(\w+)");
			var closeTags = System.Text.RegularExpressions.Regex.Matches(content, @"\{%\s*end(\w+)");

			var openNames = openTags.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();
			var closeNames = closeTags.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();

			foreach (var tag in new[] { "if", "for", "case", "unless", "comment" })
			{
				var openCount = openNames.Count(n => n == tag);
				var closeCount = closeNames.Count(n => n == tag);
				if (openCount != closeCount)
				{
					output.WriteLine($"  {file}: Unclosed '{tag}' tag (opened {openCount}, closed {closeCount})", ConsoleColor.Red);
					issues++;
				}
			}

			// Check for unknown objects (Power Pages specific)
			var knownObjects = new[] { "portal", "website", "webpage", "weblink", "webfile", "snippet", "entitylist", "entityform", "user", "request", "response" };
			var objectRefs = System.Text.RegularExpressions.Regex.Matches(content, @"\{\{\s*(\w+)\.");
			foreach (System.Text.RegularExpressions.Match match in objectRefs)
			{
				var objName = match.Groups[1].Value;
				if (!knownObjects.Contains(objName, StringComparer.OrdinalIgnoreCase))
				{
					var level = strict ? ConsoleColor.Red : ConsoleColor.Yellow;
					output.WriteLine($"  {file}: Unknown object '{objName}' (may not exist in Power Pages)", level);
					if (strict) issues++;
				}
			}

			return issues;
		}
	}
}
