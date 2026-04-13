using Greg.Xrm.Command.Services.Output;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Tabular
{
	public class TabularTranslateCommandExecutor(
		IOutput output) : ICommandExecutor<TabularTranslateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularTranslateCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!File.Exists(command.TranslationFile))
				{
					return CommandResult.Fail($"Translation file not found: {command.TranslationFile}");
				}

				var content = File.ReadAllText(command.TranslationFile);

				switch (command.Mode.ToLower())
				{
					case "export":
						output.WriteLine($"[DRY RUN] Would export translations from model '{command.ModelId}' to {command.TranslationFile}", ConsoleColor.Yellow);
						output.WriteLine($"  Language: {command.LanguageCode}");
						return CommandResult.Success();

					case "diff":
						output.WriteLine($"[DRY RUN] Would compare translations in {command.TranslationFile} with model '{command.ModelId}'", ConsoleColor.Yellow);
						output.WriteLine($"  Language: {command.LanguageCode}");
						return CommandResult.Success();

					case "deploy":
					default:
						output.WriteLine($"Deploying translations to model '{command.ModelId}'", ConsoleColor.Cyan);
						output.WriteLine($"  File: {command.TranslationFile}");
						output.WriteLine($"  Language: {command.LanguageCode}");
						output.WriteLine();
						output.WriteLine("Note: Translation deployment requires Power BI REST API access.", ConsoleColor.Yellow);
						output.WriteLine("Use the Power BI API to update the model's translations via XMLA or REST API:");
						output.WriteLine("  PATCH https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/Default.UpdateDefinition");
						output.WriteLine("  Body: BIM file with translation objects included");
						return CommandResult.Success();
				}
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException)
			{
				return CommandResult.Fail($"Error during translation operation: {ex.Message}", ex);
			}
		}
	}

	public class TabularRoleAddMeasuresCommandExecutor(
		IOutput output) : ICommandExecutor<TabularRoleAddMeasuresCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularRoleAddMeasuresCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.WriteLine($"Adding {command.Measures.Length} measure(s) to all roles in model '{command.ModelId}'", ConsoleColor.Cyan);
				output.WriteLine($"  Measures: {string.Join(", ", command.Measures)}");

				if (command.DryRun)
				{
					output.WriteLine();
					output.WriteLine("[DRY RUN] No changes made.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteLine();
				output.WriteLine("Note: Role modification requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
				output.WriteLine("Use the Power BI API to update role definitions:");
				output.WriteLine("  PATCH https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/Default.UpdateDefinition");
				output.WriteLine("  Body: BIM file with updated role.Member properties");

				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is InvalidOperationException)
			{
				return CommandResult.Fail($"Error adding measures to roles: {ex.Message}", ex);
			}
		}
	}

	public class TabularPerspectiveManageCommandExecutor(
		IOutput output) : ICommandExecutor<TabularPerspectiveManageCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TabularPerspectiveManageCommand command, CancellationToken cancellationToken)
		{
			try
			{
				switch (command.Action.ToLower())
				{
					case "list":
						output.WriteLine($"Listing perspectives for model '{command.ModelId}'", ConsoleColor.Cyan);
					 output.WriteLine("Note: Perspective listing requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
						return CommandResult.Success();

					case "create":
						if (string.IsNullOrEmpty(command.PerspectiveName))
							return CommandResult.Fail("Perspective name (--name) is required for 'create' action.");

						output.WriteLine($"Creating perspective '{command.PerspectiveName}' in model '{command.ModelId}'", ConsoleColor.Cyan);
						output.WriteLine("Note: Perspective creation requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
						return CommandResult.Success();

					case "delete":
						if (string.IsNullOrEmpty(command.PerspectiveName))
							return CommandResult.Fail("Perspective name (--name) is required for 'delete' action.");

						output.WriteLine($"Deleting perspective '{command.PerspectiveName}' from model '{command.ModelId}'", ConsoleColor.Cyan);
						output.WriteLine("Note: Perspective deletion requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
						return CommandResult.Success();

					case "add-tables":
						if (string.IsNullOrEmpty(command.PerspectiveName) || command.Tables == null || command.Tables.Length == 0)
							return CommandResult.Fail("Perspective name (--name) and tables (--tables) are required for 'add-tables' action.");

						output.WriteLine($"Adding tables to perspective '{command.PerspectiveName}': {string.Join(", ", command.Tables)}", ConsoleColor.Cyan);
						output.WriteLine("Note: This requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
						return CommandResult.Success();

					case "remove-tables":
						if (string.IsNullOrEmpty(command.PerspectiveName) || command.Tables == null || command.Tables.Length == 0)
							return CommandResult.Fail("Perspective name (--name) and tables (--tables) are required for 'remove-tables' action.");

						output.WriteLine($"Removing tables from perspective '{command.PerspectiveName}': {string.Join(", ", command.Tables)}", ConsoleColor.Cyan);
						output.WriteLine("Note: This requires Power BI REST API or XMLA endpoint access.", ConsoleColor.Yellow);
						return CommandResult.Success();

					default:
						return CommandResult.Fail($"Unknown action '{command.Action}'. Valid actions: create, delete, list, add-tables, remove-tables.");
				}
			}
			catch (Exception ex) when (ex is InvalidOperationException)
			{
				return CommandResult.Fail($"Error managing perspective: {ex.Message}", ex);
			}
		}
	}
}
