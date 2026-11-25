using System.Xml;
using System.Xml.Linq;
using Spectre.Console;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Ribbon;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class SetRibbonDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IRibbonDiffService ribbonService,
		IAnsiConsole console,
		IPublishXmlBuilder publisher) : ICommandExecutor<SetRibbonDiffCommand>
	{
		// An empty ribbon node publishes only the application ribbon.
		private const string ApplicationRibbonPublishXml = "<importexportxml><ribbons><ribbon></ribbon></ribbons></importexportxml>";

		public async Task<CommandResult> ExecuteAsync(SetRibbonDiffCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(command.BackupFile))
				{
					var path = Path.GetFullPath(command.BackupFile);
					if (!Directory.Exists(Path.GetDirectoryName(path)) || Directory.Exists(path))
						return CommandResult.Fail($"Invalid backup file path <{command.BackupFile}>. Specify a file in an existing directory.");
				}
				output.Write("Reading RibbonDiffXml from '").Write(command.FileName, ConsoleColor.Yellow).Write("'...");
				using var text = new StringReader(await File.ReadAllTextAsync(command.FileName, cancellationToken));
				using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true });
				var replacement = XElement.Load(reader, LoadOptions.SetLineInfo);
				if (replacement.Name != "RibbonDiffXml")
					return CommandResult.Fail("The input file must have a RibbonDiffXml root element. The expanded XML from ribbon get cannot be imported.");
				ribbonService.ValidateXml(replacement, command.TableName);
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);
				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (!string.IsNullOrWhiteSpace(solutionName))
						output.WriteLine($"Using default solution <{solutionName}>. For ribbon diffs, consider a dedicated solution with only the target table (no metadata or assets), e.g. --solution RibbonDiff.", ConsoleColor.Yellow);
				}
				if (string.IsNullOrWhiteSpace(solutionName))
					return CommandResult.Fail("No solution specified and no default solution found. Provide a small ribbon solution with --solution RibbonDiff.");

				output.Write("Retrieving solution '").Write(solutionName, ConsoleColor.Yellow).Write("'...");
				var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				output.WriteLine("Done", ConsoleColor.Green);
				if (solution == null) return CommandResult.Fail($"Solution <{solutionName}> was not found.");
				if (solution.ismanaged) return CommandResult.Fail($"Solution <{solutionName}> is managed. Specify an unmanaged solution for ribbon editing.");

				output.Write("Validating ribbon solution...");
				var (componentId, componentType) = await ribbonService.ResolveComponentAsync(crm, command.TableName, cancellationToken);
				var validationError = await ribbonService.ValidateSolutionAsync(crm, solution.Id, solutionName, componentId, componentType, cancellationToken);
				if (validationError != null) return CommandResult.Fail(validationError);
				output.WriteLine("Done", ConsoleColor.Green);

				output.WriteLine("Warning: this will replace the current unmanaged RibbonDiffXml in the target environment.", ConsoleColor.Yellow);
				output.Write("Exporting solution '").Write(solutionName, ConsoleColor.Yellow).Write("'...");
				var response = (ExportSolutionResponse)await crm.ExecuteAsync(new ExportSolutionRequest
				{
					SolutionName = solution.uniquename, Managed = false
				}, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				var original = ribbonService.Read(response.ExportSolutionFile, command.TableName);

				// A requested backup is part of the contract even when there is nothing to import.
				if (!string.IsNullOrWhiteSpace(command.BackupFile))
				{
					output.Write("Backing up original RibbonDiffXml to '").Write(command.BackupFile, ConsoleColor.Yellow).Write("'...");
					await File.WriteAllTextAsync(command.BackupFile, original.ToString(), cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				if (XNode.DeepEquals(original, replacement))
				{
					output.WriteLine("RibbonDiffXml is unchanged. No import needed.");
					return Success(command, solutionName, changed: false);
				}

				WriteDiff(original, replacement);
				if (string.IsNullOrWhiteSpace(command.BackupFile) &&
					(!console.Profile.Capabilities.Interactive ||
					!await console.ConfirmAsync("Replace this ribbon in the target environment?", defaultValue: false, cancellationToken)))
					return CommandResult.Fail("Aborted. The ribbon was not replaced.");

				var updatedSolution = ribbonService.Replace(response.ExportSolutionFile, command.TableName, replacement);
				output.Write("Importing solution '").Write(solutionName, ConsoleColor.Yellow).Write("'...");
				await crm.ExecuteAsync(new ImportSolutionRequest
				{
					CustomizationFile = updatedSolution, OverwriteUnmanagedCustomizations = true
				}, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Publishing ribbon customizations...");
				PublishXmlRequest publishRequest;
				if (string.IsNullOrWhiteSpace(command.TableName))
					publishRequest = new PublishXmlRequest { ParameterXml = ApplicationRibbonPublishXml };
				else
				{
					publisher.Clear();
					publisher.AddTable(command.TableName);
					publishRequest = publisher.Build() ?? throw new InvalidOperationException("No ribbon publish request was generated.");
				}
				await crm.ExecuteAsync(publishRequest, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return Success(command, solutionName, changed: true);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static CommandResult Success(SetRibbonDiffCommand command, string solutionName, bool changed)
		{
			var result = CommandResult.Success();
			result["SolutionName"] = solutionName;
			result["TableName"] = command.TableName;
			result["Changed"] = changed;
			result["Published"] = changed;
			result["BackupFile"] = command.BackupFile ?? string.Empty;
			return result;
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
