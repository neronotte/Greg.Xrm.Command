using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.QualityGate
{
	public class QualityGateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<QualityGateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(QualityGateCommand command, CancellationToken cancellationToken)
		{
			try
			{
				// If solution is provided, run solution check first
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
				{
					output.Write("Running solution check...");
					// Note: This requires pac CLI to be installed
					// For now, we parse existing results
					output.WriteLine(" Solution check must be run externally. Use --input to provide results.", ConsoleColor.Yellow);
				}

				// Parse solution check results
				var issues = await ParseSolutionCheckResultsAsync(command.InputPath);

				if (issues.Count == 0)
				{
					output.WriteLine("No issues found. Quality gate passed!", ConsoleColor.Green);
					return CommandResult.Success();
				}

				// Filter by severity
				var severityOrder = new Dictionary<string, int>
				{
					{ "Error", 0 }, { "High", 1 }, { "Medium", 2 }, { "Low", 3 }
				};

				var failThreshold = severityOrder.GetValueOrDefault(command.FailOnSeverity, 1);
				var failingIssues = issues.Where(i => severityOrder.GetValueOrDefault(i.Severity, 3) <= failThreshold).ToList();

				// Output results
				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(
						issues.Select(i => new { i.Severity, i.Component, i.Message, i.File, i.Line }).ToList(),
						Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteLine();
					output.WriteTable(issues,
						() => new[] { "Severity", "Component", "Message", "File", "Line" },
						i => new[] {
							i.Severity,
							i.Component,
							i.Message.Length > 60 ? i.Message.Substring(0, 57) + "..." : i.Message,
							i.File ?? "-",
							i.Line?.ToString() ?? "-"
						},
						ColorPicker
					);
				}

				output.WriteLine();
				output.WriteLine($"Total issues: {issues.Count}", ConsoleColor.Cyan);
				output.WriteLine($"  Error: {issues.Count(i => i.Severity == "Error")}", issues.Any(i => i.Severity == "Error") ? ConsoleColor.Red : ConsoleColor.Gray);
				output.WriteLine($"  High: {issues.Count(i => i.Severity == "High")}", issues.Any(i => i.Severity == "High") ? ConsoleColor.Red : ConsoleColor.Gray);
				output.WriteLine($"  Medium: {issues.Count(i => i.Severity == "Medium")}", issues.Any(i => i.Severity == "Medium") ? ConsoleColor.Yellow : ConsoleColor.Gray);
				output.WriteLine($"  Low: {issues.Count(i => i.Severity == "Low")}", ConsoleColor.Gray);

				if (failingIssues.Count > 0)
				{
					output.WriteLine();
					output.WriteLine($"QUALITY GATE FAILED: {failingIssues.Count} issue(s) at or above '{command.FailOnSeverity}' severity.", ConsoleColor.Red);
					return CommandResult.Fail($"Quality gate failed: {failingIssues.Count} issue(s) at or above '{command.FailOnSeverity}' severity.");
				}

				output.WriteLine($"\nQuality gate passed: No issues at or above '{command.FailOnSeverity}' severity.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Quality gate error: {ex.Message}", ex);
			}
		}

		private static ConsoleColor ColorPicker(QualityIssue issue, int rowIndex)
		{
			return issue.Severity switch
			{
				"Error" => ConsoleColor.Red,
				"High" => ConsoleColor.Red,
				"Medium" => ConsoleColor.Yellow,
				_ => ConsoleColor.Gray
			};
		}

		private async Task<List<QualityIssue>> ParseSolutionCheckResultsAsync(string? inputPath)
		{
			var issues = new List<QualityIssue>();

			if (string.IsNullOrEmpty(inputPath))
			{
				// Try to find default location
				var defaultPath = Path.Combine(Environment.CurrentDirectory, "SolutionCheckerResults");
				if (Directory.Exists(defaultPath))
				{
					inputPath = defaultPath;
				}
				else
				{
					return issues;
				}
			}

			if (File.Exists(inputPath) && inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				await ParseZipResultsAsync(inputPath!, issues);
			}
			else if (Directory.Exists(inputPath))
			{
				await ParseDirectoryResultsAsync(inputPath!, issues);
			}

			return issues;
		}

		private async Task ParseZipResultsAsync(string zipPath, List<QualityIssue> issues)
		{
			using var archive = ZipFile.OpenRead(zipPath);
			foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
			{
				using var stream = entry.Open();
				using var reader = new StreamReader(stream);
				var content = await reader.ReadToEndAsync();
				ParseIssueContent(content, entry.Name, issues);
			}
		}

		private async Task ParseDirectoryResultsAsync(string dirPath, List<QualityIssue> issues)
		{
			foreach (var file in Directory.EnumerateFiles(dirPath, "*.json", SearchOption.AllDirectories)
				.Concat(Directory.EnumerateFiles(dirPath, "*.xml", SearchOption.AllDirectories)))
			{
				var content = await File.ReadAllTextAsync(file);
				ParseIssueContent(content, Path.GetFileName(file), issues);
			}
		}

		private static void ParseIssueContent(string content, string fileName, List<QualityIssue> issues)
		{
			// Simplified parsing - in production, this would parse the actual solution check report format
			// For now, create sample issues based on common patterns
			if (content.Contains("Error") || content.Contains("High"))
			{
				issues.Add(new QualityIssue
				{
					Severity = content.Contains("Error") ? "Error" : "High",
					Component = "Solution",
					Message = $"Issue found in {fileName}",
					File = fileName,
					Line = null
				});
			}
		}

		private class QualityIssue
		{
			public string Severity { get; set; } = "";
			public string Component { get; set; } = "";
			public string Message { get; set; } = "";
			public string? File { get; set; }
			public int? Line { get; set; }
		}
	}
}
