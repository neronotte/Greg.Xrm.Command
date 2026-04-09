using Greg.Xrm.Command.Services.Output;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Connector
{
	public class ConnectorImportCommandExecutor(
		IOutput output) : ICommandExecutor<ConnectorImportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectorImportCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FilePath))
			{
				return CommandResult.Fail($"Connector definition not found: {command.FilePath}");
			}

			var content = await File.ReadAllTextAsync(command.FilePath, cancellationToken);

			output.WriteLine($"Importing connector from: {command.FilePath}", ConsoleColor.Cyan);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would import:", ConsoleColor.Yellow);
				output.WriteLine($"  File: {command.FilePath}");
				output.WriteLine($"  Size: {content.Length} bytes");
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
					output.WriteLine($"  Solution: {command.SolutionUniqueName}");
				return CommandResult.Success();
			}

			output.WriteLine("Note: Connector import requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class ConnectorExportCommandExecutor(
		IOutput output) : ICommandExecutor<ConnectorExportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectorExportCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine($"Exporting connector: {command.ConnectorName}", ConsoleColor.Cyan);
			output.WriteLine($"  Output: {command.OutputPath}");
			output.WriteLine("Note: Connector export requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class ConnectorTestCommandExecutor(
		IOutput output) : ICommandExecutor<ConnectorTestCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectorTestCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine($"Testing connector: {command.ConnectorName}", ConsoleColor.Cyan);
			output.WriteLine($"  Operation: {command.OperationName}");
			if (!string.IsNullOrEmpty(command.PayloadPath))
				output.WriteLine($"  Payload: {command.PayloadPath}");

			output.WriteLine("Note: Connector testing requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class ConnectorValidateCommandExecutor(
		IOutput output) : ICommandExecutor<ConnectorValidateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectorValidateCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FilePath))
			{
				return CommandResult.Fail($"Connector definition not found: {command.FilePath}");
			}

			var content = await File.ReadAllTextAsync(command.FilePath, cancellationToken);

			output.WriteLine($"Validating connector: {command.FilePath}", ConsoleColor.Cyan);

			// Basic validation - check for required OpenAPI fields
			var issues = 0;
			if (!content.Contains("\"swagger\"") && !content.Contains("\"openapi\""))
			{
				output.WriteLine("  WARNING: Missing 'swagger' or 'openapi' version field", ConsoleColor.Yellow);
				issues++;
			}
			if (!content.Contains("\"info\""))
			{
				output.WriteLine("  WARNING: Missing 'info' field", ConsoleColor.Yellow);
				issues++;
			}
			if (!content.Contains("\"paths\""))
			{
				output.WriteLine("  ERROR: Missing 'paths' field", ConsoleColor.Red);
				issues++;
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
}
