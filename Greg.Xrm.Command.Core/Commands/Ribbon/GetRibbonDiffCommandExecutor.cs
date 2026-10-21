using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Ribbon;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class GetRibbonDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IRibbonDiffService ribbonService) : ICommandExecutor<GetRibbonDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetRibbonDiffCommand command, CancellationToken cancellationToken)
		{
			// Preserve pipeable XML: progress and result metadata are shown only in file mode.
			var progress = string.IsNullOrWhiteSpace(command.FileName) ? null : output;
			try
			{
				if (progress != null)
				{
					var path = Path.GetFullPath(command.FileName);
					if (!Directory.Exists(Path.GetDirectoryName(path)) || Directory.Exists(path))
						return CommandResult.Fail($"Invalid output file path <{command.FileName}>. Specify a file in an existing directory.");
				}
				progress?.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				progress?.WriteLine("Done", ConsoleColor.Green);

				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (!string.IsNullOrWhiteSpace(solutionName))
						progress?.WriteLine($"Using default solution <{solutionName}>. For ribbon diffs, consider a dedicated solution with only the target table (no metadata or assets), e.g. --solution RibbonDiff.", ConsoleColor.Yellow);
				}
				if (string.IsNullOrWhiteSpace(solutionName))
					return CommandResult.Fail("No solution specified and no default solution found. Provide a small ribbon solution with --solution RibbonDiff.");

				progress?.Write("Retrieving solution '").Write(solutionName, ConsoleColor.Yellow).Write("'...");
				var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				progress?.WriteLine("Done", ConsoleColor.Green);
				if (solution == null) return CommandResult.Fail($"Solution <{solutionName}> was not found.");
				if (solution.ismanaged) return CommandResult.Fail($"Solution <{solutionName}> is managed and cannot be exported. Specify an unmanaged solution.");

				progress?.Write("Validating ribbon solution...");
				var (componentId, componentType) = await ribbonService.ResolveComponentAsync(crm, command.TableName, cancellationToken);
				var validationError = await ribbonService.ValidateSolutionAsync(crm, solution.Id, solutionName, componentId, componentType, cancellationToken);
				if (validationError != null) return CommandResult.Fail(validationError);
				progress?.WriteLine("Done", ConsoleColor.Green);

				progress?.Write("Exporting solution '").Write(solutionName, ConsoleColor.Yellow).Write("'...");
				var response = (ExportSolutionResponse)await crm.ExecuteAsync(new ExportSolutionRequest
				{
					SolutionName = solution.uniquename, Managed = false
				}, cancellationToken);
				progress?.WriteLine("Done", ConsoleColor.Green);
				var xml = ribbonService.Read(response.ExportSolutionFile, command.TableName).ToString();
				if (progress == null)
				{
					output.WriteRawLine(xml);
					// CommandRunnerBase prints nonempty results, which would corrupt redirected XML.
					return CommandResult.Success();
				}

				output.Write("Saving RibbonDiffXml to '").Write(command.FileName, ConsoleColor.Yellow).Write("'...");
				await File.WriteAllTextAsync(command.FileName, xml, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				var result = CommandResult.Success();
				result["SolutionName"] = solutionName;
				result["TableName"] = command.TableName;
				result["OutputFile"] = command.FileName;
				return result;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
