using Greg.Xrm.Command.Services.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	public class PluginStepScanCommandExecutor(
		IOutput output) : ICommandExecutor<PluginStepScanCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PluginStepScanCommand command, CancellationToken cancellationToken)
		{
			try
			{
				IList<PluginAssemblyMetadata> assemblies;
				if (File.Exists(command.Path))
				{
					var meta = PluginScanner.ScanAssembly(command.Path);
					assemblies = meta != null ? new List<PluginAssemblyMetadata> { meta } : Array.Empty<PluginAssemblyMetadata>();
				}
				else if (Directory.Exists(command.Path))
				{
					assemblies = PluginScanner.ScanDirectory(command.Path);
				}
				else
				{
					return CommandResult.Fail($"Path not found: {command.Path}");
				}

				if (assemblies.Count == 0)
				{
					output.WriteLine("No plugin attributes found in the specified path.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				var warnings = new List<string>();
				var totalSteps = 0;
				var totalImages = 0;
				var totalWebhooks = 0;

				foreach (var asm in assemblies)
				{
					foreach (var pluginType in asm.PluginTypes)
					{
						foreach (var step in pluginType.Steps)
						{
							totalSteps++;
							var stepWarnings = ValidateStep(step, pluginType);
							warnings.AddRange(stepWarnings);
						}

						totalImages += pluginType.Images.Count;
						totalWebhooks += pluginType.Webhooks.Count;
					}
				}

				if (command.Format == "json")
				{
					var report = new
					{
						Assemblies = assemblies.Count,
						TotalSteps = totalSteps,
						TotalImages = totalImages,
						TotalWebhooks = totalWebhooks,
						Warnings = warnings,
						Status = warnings.Count == 0 ? "PASS" : "WARN",
					};
					output.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
				}
				else
				{
					output.WriteLine($"Plugin Step Scan Results:", ConsoleColor.Cyan);
					output.WriteLine($"  Assemblies scanned: {assemblies.Count}");
					output.WriteLine($"  Plugin types found: {assemblies.Sum(a => a.PluginTypes.Count)}");
					output.WriteLine($"  Total steps: {totalSteps}");
					output.WriteLine($"  Total images: {totalImages}");
					output.WriteLine($"  Total webhooks: {totalWebhooks}");
					output.WriteLine();

					if (warnings.Count > 0)
					{
						output.WriteLine($"Warnings ({warnings.Count}):", ConsoleColor.Yellow);
						foreach (var warning in warnings)
						{
							output.WriteLine($"  ⚠ {warning}");
						}
					}
					else
					{
						output.WriteLine("All plugin steps are valid.", ConsoleColor.Green);
					}
				}

				if (command.Strict && warnings.Count > 0)
				{
					return CommandResult.Fail($"{warnings.Count} validation warning(s) found.");
				}

				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException)
			{
				return CommandResult.Fail($"Error during step scan: {ex.Message}", ex);
			}
		}

		private static List<string> ValidateStep(PluginStepMetadata step, PluginTypeMetadata pluginType)
		{
			var warnings = new List<string>();
			var stepName = step.Name ?? $"{pluginType.TypeNameWithoutNamespace}_{step.Message}_{step.Entity}";

			// Validate stage
			if (step.Stage is < 10 or > 50)
			{
				warnings.Add($"[{stepName}] Stage {step.Stage} is outside the typical range (10-50). Pre-validation=10, Main=40, Post=50.");
			}

			// Validate message wildcards
			if (step.Message == "*")
			{
				warnings.Add($"[{stepName}] Wildcard message (*) will register for ALL messages. This is rarely intentional.");
			}

			// Validate entity wildcards
			if (step.Entity == "*")
			{
				warnings.Add($"[{stepName}] Wildcard entity (*) will register for ALL entities. This is rarely intentional.");
			}

			// Validate rank
			if (step.Rank < 1)
			{
				warnings.Add($"[{stepName}] Rank must be >= 1. Current: {step.Rank}");
			}

			// Validate execution mode
			if (step.ExecutionMode is < 0 or > 1)
			{
				warnings.Add($"[{stepName}] Invalid execution mode: {step.ExecutionMode}. Expected 0 (synchronous) or 1 (asynchronous).");
			}

			// Validate deployment
			if (step.Deployment is < 0 or > 2)
			{
				warnings.Add($"[{stepName}] Invalid deployment: {step.Deployment}. Expected 0 (Server), 1 (Offline), or 2 (Both).");
			}

			// Validate filtering attributes
			if (step.FilteringAttributes != null && step.FilteringAttributes.Length == 0)
			{
				warnings.Add($"[{stepName}] Empty filtering attributes array. Consider omitting the attribute to register for all attributes.");
			}

			return warnings;
		}
	}
}
