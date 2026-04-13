using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class WebResourceWatchCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWebResourceRepository webResourceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<WebResourceWatchCommand>
	{
		private readonly Dictionary<string, DateTime> _pendingChanges = new();
		private FileSystemWatcher? _watcher;
		private Timer? _debounceTimer;

		public async Task<CommandResult> ExecuteAsync(WebResourceWatchCommand command, CancellationToken cancellationToken)
		{
			try
			{
				// 1. Parse config
				if (!File.Exists(command.ConfigPath))
				{
					return CommandResult.Fail($"Config file not found: {command.ConfigPath}");
				}

				var config = ParseConfig(command.ConfigPath);
				if (config == null || config.Mappings.Count == 0)
				{
					return CommandResult.Fail("No mappings found in config file.");
				}

				// 2. Connect to Dataverse
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// 3. Resolve solution
				var solutionId = Guid.Empty;
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
				{
					var solution = await solutionRepository.GetByNameAsync(command.SolutionUniqueName, cancellationToken);
					if (solution == null)
					{
						return CommandResult.Fail($"Solution '{command.SolutionUniqueName}' not found.");
					}
					solutionId = solution.Id;
				}

				// 4. Initial sync
				output.WriteLine("Performing initial sync...", ConsoleColor.Cyan);
				foreach (var mapping in config.Mappings)
				{
					if (!File.Exists(mapping.LocalPath))
					{
						output.WriteLine($"  SKIP: {mapping.LocalPath} (file not found)", ConsoleColor.Yellow);
						continue;
					}

					await UploadWebResourceAsync(crm, mapping, solutionId, true, cancellationToken);
				}
				output.WriteLine("Initial sync complete.", ConsoleColor.Green);

				// 5. Set up file watching
				output.WriteLine();
				output.WriteLine("Watching for file changes. Press Ctrl+C to stop.", ConsoleColor.Yellow);
				output.WriteLine();

				if (command.Poll)
				{
					await WatchWithPollingAsync(crm, config, solutionId, command.PollIntervalMs, command.Publish, cancellationToken);
				}
				else
				{
					await WatchWithFileSystemWatcherAsync(crm, config, solutionId, command.DebounceMs, command.Publish, cancellationToken);
				}

				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException)
			{
				return CommandResult.Fail($"Error during web resource watch: {ex.Message}", ex);
			}
			finally
			{
				_watcher?.Dispose();
				_debounceTimer?.Dispose();
			}
		}

		private WebResourceMapConfig? ParseConfig(string configPath)
		{
			var content = File.ReadAllText(configPath);
			return JsonSerializer.Deserialize<WebResourceMapConfig>(content);
		}

		private async Task WatchWithFileSystemWatcherAsync(IOrganizationServiceAsync2 crm, WebResourceMapConfig config, Guid solutionId, int debounceMs, bool publish, CancellationToken ct)
		{
			// Group mappings by directory
			var dirMappings = config.Mappings
				.GroupBy(m => Path.GetDirectoryName(Path.GetFullPath(m.LocalPath))!)
				.ToDictionary(g => g.Key, g => g.ToList());

			foreach (var (dir, mappings) in dirMappings)
			{
				if (!Directory.Exists(dir)) continue;

				var watcher = new FileSystemWatcher(dir)
				{
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
					Filter = "*.*",
					IncludeSubdirectories = true,
				};

				watcher.Changed += (s, e) => OnFileChanged(e.FullPath, mappings, ct);
				watcher.Created += (s, e) => OnFileChanged(e.FullPath, mappings, ct);

				watcher.EnableRaisingEvents = true;
				_watcher = watcher; // Keep reference for disposal
			}

			// Set up debounce timer
			_debounceTimer = new Timer(async _ => await FlushPendingChangesAsync(crm, config, solutionId, publish, ct), null, Timeout.Infinite, Timeout.Infinite);

			// Keep running until cancellation
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(1000, ct);
			}
		}

		private void OnFileChanged(string filePath, List<WebResourceMapping> mappings, CancellationToken ct)
		{
			var mapping = mappings.FirstOrDefault(m => Path.GetFullPath(m.LocalPath) == Path.GetFullPath(filePath));
			if (mapping == null) return;

			lock (_pendingChanges)
			{
				_pendingChanges[Path.GetFullPath(filePath)] = DateTime.UtcNow;
			}

			// Reset debounce timer
			_debounceTimer?.Change(500, Timeout.Infinite);

			output.WriteLine($"[{DateTime.Now:HH:mm:ss}] Detected change: {Path.GetFileName(filePath)}", ConsoleColor.DarkCyan);
		}

		private async Task WatchWithPollingAsync(IOrganizationServiceAsync2 crm, WebResourceMapConfig config, Guid solutionId, int intervalMs, bool publish, CancellationToken ct)
		{
			var lastModified = config.Mappings
				.Where(m => File.Exists(m.LocalPath))
				.ToDictionary(m => Path.GetFullPath(m.LocalPath), m => File.GetLastWriteTimeUtc(m.LocalPath));

			output.WriteLine($"Polling every {intervalMs}ms for changes...", ConsoleColor.DarkGray);

			while (!ct.IsCancellationRequested)
			{
				foreach (var mapping in config.Mappings)
				{
					if (!File.Exists(mapping.LocalPath)) continue;

					var fullPath = Path.GetFullPath(mapping.LocalPath);
					var lastWrite = File.GetLastWriteTimeUtc(fullPath);

					if (!lastModified.TryGetValue(fullPath, out var previous) || lastWrite > previous)
					{
						lastModified[fullPath] = lastWrite;
						output.WriteLine($"[{DateTime.Now:HH:mm:ss}] Detected change: {Path.GetFileName(fullPath)}", ConsoleColor.DarkCyan);
						await UploadWebResourceAsync(crm, mapping, solutionId, true, ct);

						if (publish)
						{
							await PublishAsync(crm, ct);
						}
					}
				}

				await Task.Delay(intervalMs, ct);
			}
		}

		private async Task FlushPendingChangesAsync(IOrganizationServiceAsync2 crm, WebResourceMapConfig config, Guid solutionId, bool publish, CancellationToken ct)
		{
			List<string> changedPaths;
			lock (_pendingChanges)
			{
				changedPaths = _pendingChanges.Keys.ToList();
				_pendingChanges.Clear();
			}

			if (changedPaths.Count == 0) return;

			foreach (var path in changedPaths)
			{
				var mapping = config.Mappings.FirstOrDefault(m => Path.GetFullPath(m.LocalPath) == path);
				if (mapping == null) continue;

				await UploadWebResourceAsync(crm, mapping, solutionId, true, ct);
			}

			if (publish)
			{
				await PublishAsync(crm, ct);
			}
		}

		private async Task PublishAsync(IOrganizationServiceAsync2 crm, CancellationToken ct)
		{
			output.Write("  Publishing...");
			var publishRequest = new PublishAllXmlRequest();
			await crm.ExecuteAsync(publishRequest, ct);
			output.WriteLine(" Done", ConsoleColor.Green);
		}

		private async Task UploadWebResourceAsync(IOrganizationServiceAsync2 crm, WebResourceMapping mapping, Guid solutionId, bool force, CancellationToken ct)
		{
			var content = await File.ReadAllBytesAsync(mapping.LocalPath, ct);
			var base64Content = Convert.ToBase64String(content);
			var webResourceType = GetWebResourceTypeNumber(mapping.UniqueName);

			var query = new QueryExpression("webresource");
			query.ColumnSet.AddColumn("webresourceid");
			query.ColumnSet.AddColumn("content");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, mapping.UniqueName);

			var result = await crm.RetrieveMultipleAsync(query, ct);

			var wr = new Entity("webresource");
			wr["uniquename"] = mapping.UniqueName;
			wr["name"] = mapping.DisplayName ?? mapping.UniqueName;
			wr["webresourcetype"] = new OptionSetValue(webResourceType);
			wr["content"] = base64Content;

			if (solutionId != Guid.Empty)
			{
				wr["solutionid"] = new EntityReference("solution", solutionId);
			}

			if (result.Entities.Count > 0)
			{
				var existingId = result.Entities[0].Id;
				var existingContent = result.Entities[0].GetAttributeValue<string>("content");

				if (existingContent == base64Content)
				{
					return; // No changes
				}

				wr.Id = existingId;
				await crm.UpdateAsync(wr, ct);
				output.WriteLine($"  UPDATED: {mapping.UniqueName} <- {mapping.LocalPath}", ConsoleColor.Green);
			}
			else
			{
				wr.Id = await crm.CreateAsync(wr, ct);
				output.WriteLine($"  CREATED: {mapping.UniqueName} <- {mapping.LocalPath}", ConsoleColor.Green);
			}
		}

		private static int GetWebResourceTypeNumber(string uniqueName)
		{
			var ext = Path.GetExtension(uniqueName).ToLowerInvariant();
			return ext switch
			{
				".html" or ".htm" => 1,
				".css" => 2,
				".js" => 3,
				".xml" => 4,
				".png" => 5,
				".jpg" or ".jpeg" => 6,
				".gif" => 7,
				".xap" => 8,
				".xsl" => 9,
				".ico" => 10,
				".svg" => 11,
				".resx" => 12,
				_ => 3,
			};
		}
	}
}
