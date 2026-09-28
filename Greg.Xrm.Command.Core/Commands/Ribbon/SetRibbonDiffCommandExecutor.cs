using System.Xml.Linq;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class SetRibbonDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<SetRibbonDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SetRibbonDiffCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FileName))
				return CommandResult.Fail($"Ribbon XML file <{command.FileName}> does not exist.");
			if (!string.IsNullOrWhiteSpace(command.BackupFile))
			{
				try
				{
					var backupPath = Path.GetFullPath(command.BackupFile);
					if (!Directory.Exists(Path.GetDirectoryName(backupPath)))
						return CommandResult.Fail($"Backup file directory for <{command.BackupFile}> does not exist.");
					if (File.Exists(backupPath))
						return CommandResult.Fail($"Backup file <{command.BackupFile}> already exists. Choose a new path to avoid overwriting it.");
				}
				catch (Exception ex)
				{
					return CommandResult.Fail($"Invalid backup file path <{command.BackupFile}>: {ex.Message}", ex);
				}
			}

			XElement replacement;
			try
			{
				replacement = XElement.Parse(await File.ReadAllTextAsync(command.FileName, cancellationToken));
				if (replacement.Name != "RibbonDiffXml")
					return CommandResult.Fail("The input file must have a RibbonDiffXml root element. The expanded XML from ribbon get cannot be imported.");
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Unable to read RibbonDiffXml: {ex.Message}", ex);
			}

			try
			{
				output.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				var (componentId, componentType) = await RibbonDiffExport.ResolveComponentAsync(crm, command.TableName, cancellationToken);

				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (!string.IsNullOrWhiteSpace(solutionName))
					output.WriteLine($"Using default solution <{solutionName}>. For ribbon diffs, consider a dedicated solution with only the target table (no metadata or assets), e.g. --solution RibbonDiff.", ConsoleColor.Yellow);
				}
				if (string.IsNullOrWhiteSpace(solutionName))
					return CommandResult.Fail("No solution specified and no default solution found. Provide a small ribbon solution with --solution RibbonDiff.");
				var sourceSolution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				if (sourceSolution == null)
					return CommandResult.Fail($"Solution <{solutionName}> was not found.");
				if (sourceSolution.ismanaged)
					return CommandResult.Fail($"Solution <{solutionName}> is managed. Specify an unmanaged solution for ribbon editing.");
				var validationError = await RibbonDiffExport.ValidateSolutionAsync(
					crm, sourceSolution.Id, solutionName, componentId, componentType, cancellationToken);
				if (validationError != null)
					return CommandResult.Fail(validationError);

				output.WriteLine("Warning: this will replace the current unmanaged RibbonDiffXml in the target environment.", ConsoleColor.Yellow);
				output.Write($"Exporting solution <{solutionName}>...");
				var exportResponse = (ExportSolutionResponse)await crm.ExecuteAsync(new ExportSolutionRequest
				{
				SolutionName = sourceSolution.uniquename,
				Managed = false
				}, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				using var archive = new SolutionZipArchive(exportResponse.ExportSolutionFile);

				XElement? original = null;
				archive.UpdateEntryXml("customizations.xml", document =>
				{
					original = RibbonDiffExport.FindRibbonDiff(document, command.TableName);
					return false;
				});
				if (original == null)
					return CommandResult.Fail("RibbonDiffXml was not found in the exported temporary solution.");

				if (XNode.DeepEquals(original, replacement))
				{
					output.WriteLine("RibbonDiffXml is unchanged. No import needed.");
					return CommandResult.Success();
				}

				if (!string.IsNullOrWhiteSpace(command.BackupFile))
				{
					await using var backupStream = new FileStream(command.BackupFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);
					await using var backupWriter = new StreamWriter(backupStream);
					await backupWriter.WriteAsync(original.ToString().AsMemory(), cancellationToken);
					await backupWriter.FlushAsync(cancellationToken);
					output.WriteLine($"Original RibbonDiffXml saved to {command.BackupFile}", ConsoleColor.DarkGray);
				}

				WriteDiff(original, replacement);
				if (string.IsNullOrWhiteSpace(command.BackupFile))
				{
					output.Write("Replace this ribbon in the target environment? (y/N) ");
					if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
					{
						output.WriteLine("Aborted.", ConsoleColor.Yellow);
						return CommandResult.Success();
					}
				}

				ReplaceRibbonDiff(archive, command.TableName, replacement);
				output.Write($"Importing solution <{solutionName}>...");
				await crm.ExecuteAsync(new ImportSolutionRequest
				{
					CustomizationFile = archive.ToArray(),
					OverwriteUnmanagedCustomizations = true
				}, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Publishing ribbon customizations...");
				OrganizationRequest publishRequest;
				if (string.IsNullOrWhiteSpace(command.TableName))
					publishRequest = new PublishAllXmlRequest();
				else
				{
					var builder = new PublishXmlBuilder();
					builder.AddTable(command.TableName);
					publishRequest = builder.Build()!;
				}
				await crm.ExecuteAsync(publishRequest, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static void ReplaceRibbonDiff(SolutionZipArchive archive, string tableName, XElement replacement)
		{
			archive.UpdateEntryXml("customizations.xml", document =>
			{
				var element = RibbonDiffExport.FindRibbonDiff(document, tableName)
					?? throw new InvalidOperationException("RibbonDiffXml was not found in the exported temporary solution.");
				element.ReplaceWith(new XElement(replacement));
				return true;
			});
		}

		private void WriteDiff(XElement oldXml, XElement newXml)
		{
			output.WriteLine("RibbonDiffXml changes (- current, + new):", ConsoleColor.Yellow);
			var oldLines = oldXml.ToString().Split('\n');
			var newLines = newXml.ToString().Split('\n');
			var prefix = 0;
			while (prefix < oldLines.Length && prefix < newLines.Length && oldLines[prefix] == newLines[prefix]) prefix++;
			var oldEnd = oldLines.Length - 1;
			var newEnd = newLines.Length - 1;
			while (oldEnd >= prefix && newEnd >= prefix && oldLines[oldEnd] == newLines[newEnd]) { oldEnd--; newEnd--; }
			for (var i = prefix; i <= oldEnd; i++) output.WriteLine("- " + oldLines[i], ConsoleColor.Red);
			for (var i = prefix; i <= newEnd; i++) output.WriteLine("+ " + newLines[i], ConsoleColor.Green);
		}
	}
}
