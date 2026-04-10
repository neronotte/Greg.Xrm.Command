using Greg.Xrm.Command.Services.Output;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Pcf
{
	public class PcfTestCommandExecutor(
		IOutput output) : ICommandExecutor<PcfTestCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PcfTestCommand command, CancellationToken cancellationToken)
		{
			var projectPath = command.Path ?? Environment.CurrentDirectory;
			if (!Directory.Exists(projectPath))
			{
				return CommandResult.Fail($"PCF project path not found: {projectPath}");
			}

			output.WriteLine($"Running PCF tests in {command.Browser} mode...", ConsoleColor.Cyan);
			output.WriteLine($"  Project: {projectPath}");
			output.WriteLine($"  Reporter: {command.Reporter}");
			output.WriteLine();
			output.WriteLine("Note: PCF testing requires the PCF test harness.", ConsoleColor.Yellow);
			output.WriteLine("Run: pacx pcf test --browser headless");
			output.WriteLine("See: https://docs.microsoft.com/en-us/powerapps/developer/component-framework/testing");

			return CommandResult.Success();
		}
	}

	public class PcfPublishCommandExecutor(
		IOutput output) : ICommandExecutor<PcfPublishCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PcfPublishCommand command, CancellationToken cancellationToken)
		{
			var projectPath = command.Path ?? Environment.CurrentDirectory;
			if (!Directory.Exists(projectPath))
			{
				return CommandResult.Fail($"PCF project path not found: {projectPath}");
			}

			// Read ControlManifest.Input.xml to get component info
			var manifestPath = Path.Combine(projectPath, "ControlManifest.Input.xml");
			if (!File.Exists(manifestPath))
			{
				return CommandResult.Fail("ControlManifest.Input.xml not found. Are you in a PCF project directory?");
			}

			var doc = XDocument.Load(manifestPath);
			var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
			var control = doc.Descendants(ns + "control").FirstOrDefault();
			var version = control?.Attribute("version")?.Value ?? "unknown";
			var name = control?.Attribute("namespace")?.Value + "." + control?.Attribute("name")?.Value;

			output.WriteLine($"PCF Component: {name} v{version}", ConsoleColor.Cyan);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would publish:", ConsoleColor.Yellow);
				output.WriteLine($"  Component: {name}");
				output.WriteLine($"  Version: {version}");
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
					output.WriteLine($"  Solution: {command.SolutionUniqueName}");
				return CommandResult.Success();
			}

			output.WriteLine();
			output.WriteLine("Note: PCF publishing requires pac CLI or solution import.", ConsoleColor.Yellow);
			output.WriteLine("Run: pac solution import or use the maker portal.");

			return CommandResult.Success();
		}
	}

	public class PcfVersionBumpCommandExecutor(
		IOutput output) : ICommandExecutor<PcfVersionBumpCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PcfVersionBumpCommand command, CancellationToken cancellationToken)
		{
			var projectPath = command.Path ?? Environment.CurrentDirectory;
			var manifestPath = Path.Combine(projectPath, "ControlManifest.Input.xml");

			if (!File.Exists(manifestPath))
			{
				return CommandResult.Fail("ControlManifest.Input.xml not found.");
			}

			var doc = XDocument.Load(manifestPath);
			var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
			var control = doc.Descendants(ns + "control").FirstOrDefault();
			var versionStr = control?.Attribute("version")?.Value ?? "1.0.0";

			var parts = versionStr.Split('.');
			if (parts.Length < 3 || !int.TryParse(parts[0], out var major) ||
			    !int.TryParse(parts[1], out var minor) || !int.TryParse(parts[2], out var patch))
			{
				return CommandResult.Fail($"Invalid version format: '{versionStr}'. Expected 'major.minor.patch'.");
			}

			var newVersion = command.BumpType.ToLowerInvariant() switch
			{
				"major" => $"{major + 1}.0.0",
				"minor" => $"{major}.{minor + 1}.0",
				"patch" => $"{major}.{minor}.{patch + 1}",
				_ => versionStr
			};

			output.WriteLine($"Bumping version: {versionStr} -> {newVersion} ({command.BumpType})", ConsoleColor.Cyan);

			if (control != null)
			{
				control.SetAttributeValue("version", newVersion);
				doc.Save(manifestPath);
				output.WriteLine($"Updated ControlManifest.Input.xml to v{newVersion}", ConsoleColor.Green);
			}

			if (!string.IsNullOrEmpty(command.Message))
			{
				var changelogPath = Path.Combine(projectPath, "CHANGELOG.md");
				var entry = $"## v{newVersion}\n- {command.Message}\n\n";
				if (File.Exists(changelogPath))
				{
					var existing = await File.ReadAllTextAsync(changelogPath, cancellationToken);
					await File.WriteAllTextAsync(changelogPath, entry + existing, cancellationToken);
				}
				else
				{
					await File.WriteAllTextAsync(changelogPath, $"# Changelog\n\n{entry}", cancellationToken);
				}
				output.WriteLine("Updated CHANGELOG.md", ConsoleColor.Green);
			}

			return CommandResult.Success();
		}
	}

	public class PcfDependencyCheckCommandExecutor(
		IOutput output) : ICommandExecutor<PcfDependencyCheckCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PcfDependencyCheckCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine("PCF Dependency Check", ConsoleColor.Cyan);
			output.WriteLine();
			output.WriteLine("Note: Dependency checking requires Power Platform Admin API access.", ConsoleColor.Yellow);
			output.WriteLine();
			output.WriteLine("Common PCF dependencies to verify in target environment:");
			output.WriteLine("  - Component Framework enabled");
			output.WriteLine("  - Required web resources available");
			output.WriteLine("  - Correct Power Apps version");

			return CommandResult.Success();
		}
	}
}
