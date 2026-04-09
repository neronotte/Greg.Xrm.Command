using Greg.Xrm.Command.Services.Output;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Tabular
{
	public class TabularDeployCommandExecutor(
		IOutput output) : ICommandExecutor<TabularDeployCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularDeployCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.BimFilePath))
			{
				return CommandResult.Fail($"BIM file not found: {command.BimFilePath}");
			}

			var bimContent = await File.ReadAllTextAsync(command.BimFilePath, cancellationToken);
			var bim = JsonDocument.Parse(bimContent);

			var modelName = bim.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown";
			var compatLevel = bim.RootElement.TryGetProperty("compatibilityLevel", out var compatProp) ? compatProp.GetInt32() : 0;

			output.WriteLine($"Tabular Deploy: {modelName}", ConsoleColor.Cyan);
			output.WriteLine($"  BIM File: {command.BimFilePath}");
			output.WriteLine($"  Workspace: {command.Workspace}");
			output.WriteLine($"  Mode: {command.Mode}");
			output.WriteLine($"  Compatibility Level: {compatLevel}");

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would deploy:", ConsoleColor.Yellow);
				output.WriteLine($"  Model: {modelName}");
				output.WriteLine($"  Tables: {bim.RootElement.TryGetProperty("tables", out var tables) ? tables.GetArrayLength() : 0}");
				output.WriteLine($"  Measures: {CountMeasures(bim)}");
				return CommandResult.Success();
			}

			output.WriteLine();
			output.WriteLine("Note: Tabular deploy requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
			output.WriteLine("  Premium/Embedded: Use XMLA endpoint (Analysis Services)");
			output.WriteLine("  Pro: Use Power BI REST API (limited TOM support)");

			return CommandResult.Success();
		}

		private static int CountMeasures(JsonDocument bim)
		{
			var count = 0;
			if (bim.RootElement.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
			{
				foreach (var table in tables.EnumerateArray())
				{
					if (table.TryGetProperty("measures", out var measures) && measures.ValueKind == JsonValueKind.Array)
					{
						count += measures.GetArrayLength();
					}
				}
			}
			return count;
		}
	}

	public class TabularDiffCommandExecutor(
		IOutput output) : ICommandExecutor<TabularDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularDiffCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.BimFilePath))
			{
				return CommandResult.Fail($"BIM file not found: {command.BimFilePath}");
			}

			var local = JsonDocument.Parse(await File.ReadAllTextAsync(command.BimFilePath, cancellationToken));
			output.WriteLine($"Tabular Diff: {command.BimFilePath} vs {command.Workspace}/{command.DatasetName}", ConsoleColor.Cyan);
			output.WriteLine();
			output.WriteLine("Note: Comparing against deployed model requires Power BI API access.", ConsoleColor.Yellow);

			return CommandResult.Success();
		}
	}

	public class TabularValidateCommandExecutor(
		IOutput output) : ICommandExecutor<TabularValidateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularValidateCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.BimFilePath))
			{
				return CommandResult.Fail($"BIM file not found: {command.BimFilePath}");
			}

			var bimContent = await File.ReadAllTextAsync(command.BimFilePath, cancellationToken);
			var bim = JsonDocument.Parse(bimContent);

			var issues = 0;

			// Check for tables without columns
			if (bim.RootElement.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
			{
				foreach (var table in tables.EnumerateArray())
				{
					var tableName = table.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";

					if (!table.TryGetProperty("columns", out var columns) || columns.GetArrayLength() == 0)
					{
						output.WriteLine($"  WARNING: Table '{tableName}' has no columns", ConsoleColor.Yellow);
						issues++;
					}

					// Check for measures with circular references
					if (table.TryGetProperty("measures", out var measures) && measures.ValueKind == JsonValueKind.Array)
					{
						foreach (var measure in measures.EnumerateArray())
						{
							var measureName = measure.TryGetProperty("name", out var mn) ? mn.GetString() : "Unknown";
							var expression = measure.TryGetProperty("expression", out var expr) ? expr.GetString() : "";

							// Simple circular reference check (measure referencing itself)
							if (!string.IsNullOrEmpty(expression) && expression.Contains($"[{measureName}]"))
							{
								output.WriteLine($"  ERROR: Measure '{tableName}'.'{measureName}' has circular reference", ConsoleColor.Red);
								issues++;
							}
						}
					}
				}
			}

			if (issues > 0)
			{
				output.WriteLine($"\nFound {issues} issue(s).", command.Strict ? ConsoleColor.Red : ConsoleColor.Yellow);
				return command.Strict ? CommandResult.Fail($"Validation failed: {issues} issue(s)") : CommandResult.Success();
			}

			output.WriteLine("Validation passed. No issues found.", ConsoleColor.Green);
			return CommandResult.Success();
		}
	}

	public class BimCompareCommandExecutor(
		IOutput output) : ICommandExecutor<BimCompareCommand>
	{
		public async Task<CommandResult> ExecuteAsync(BimCompareCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FileA))
				return CommandResult.Fail($"File A not found: {command.FileA}");
			if (!File.Exists(command.FileB))
				return CommandResult.Fail($"File B not found: {command.FileB}");

			var bimA = JsonDocument.Parse(await File.ReadAllTextAsync(command.FileA, cancellationToken));
			var bimB = JsonDocument.Parse(await File.ReadAllTextAsync(command.FileB, cancellationToken));

			var nameA = bimA.RootElement.TryGetProperty("name", out var na) ? na.GetString() : "Unknown";
			var nameB = bimB.RootElement.TryGetProperty("name", out var nb) ? nb.GetString() : "Unknown";

			output.WriteLine($"BIM Compare: {nameA} vs {nameB}", ConsoleColor.Cyan);
			output.WriteLine();
			output.WriteLine("Note: Full BIM comparison requires TOM library.", ConsoleColor.Yellow);
			output.WriteLine("For now, compare file sizes and table counts:");

			var tableCountA = bimA.RootElement.TryGetProperty("tables", out var ta) && ta.ValueKind == JsonValueKind.Array ? ta.GetArrayLength() : 0;
			var tableCountB = bimB.RootElement.TryGetProperty("tables", out var tb) && tb.ValueKind == JsonValueKind.Array ? tb.GetArrayLength() : 0;

			output.WriteLine($"  {Path.GetFileName(command.FileA)}: {tableCountA} tables");
			output.WriteLine($"  {Path.GetFileName(command.FileB)}: {tableCountB} tables");
			output.WriteLine($"  Difference: {tableCountA - tableCountB} tables");

			return CommandResult.Success();
		}
	}
}
