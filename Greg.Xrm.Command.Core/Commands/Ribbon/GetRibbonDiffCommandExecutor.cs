using System.Xml.Linq;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class GetRibbonDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<GetRibbonDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetRibbonDiffCommand command, CancellationToken cancellationToken)
		{
			// Without an output file the console receives only the XML, so it can be redirected or piped.
			var progress = string.IsNullOrWhiteSpace(command.FileName) ? null : output;
			try
			{
				progress?.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				progress?.WriteLine("Done", ConsoleColor.Green);

				var (componentId, componentType) = await RibbonDiffExport.ResolveComponentAsync(crm, command.TableName, cancellationToken);
				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (!string.IsNullOrWhiteSpace(solutionName))
						progress?.WriteLine($"Using default solution <{solutionName}>. For ribbon diffs, consider a dedicated solution with only the target table (no metadata or assets), e.g. --solution RibbonDiff.", ConsoleColor.Yellow);
				}
				if (string.IsNullOrWhiteSpace(solutionName))
					return CommandResult.Fail("No solution specified and no default solution found. Provide a small ribbon solution with --solution RibbonDiff.");
				var sourceSolution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				if (sourceSolution == null)
					return CommandResult.Fail($"Solution <{solutionName}> was not found.");
				if (sourceSolution.ismanaged)
					return CommandResult.Fail($"Solution <{solutionName}> is managed and cannot be exported. Specify an unmanaged solution.");
				var validationError = await RibbonDiffExport.ValidateSolutionAsync(
					crm, sourceSolution.Id, solutionName, componentId, componentType, cancellationToken);
				if (validationError != null)
					return CommandResult.Fail(validationError);

				progress?.Write($"Exporting solution <{solutionName}>...");
				var exportResponse = (ExportSolutionResponse)await crm.ExecuteAsync(new ExportSolutionRequest
				{
					SolutionName = sourceSolution.uniquename,
					Managed = false
				}, cancellationToken);
				progress?.WriteLine("Done", ConsoleColor.Green);
				using var archive = new SolutionZipArchive(exportResponse.ExportSolutionFile);
				XElement? ribbonDiff = null;
				archive.UpdateEntryXml("customizations.xml", document =>
				{
					ribbonDiff = RibbonDiffExport.FindRibbonDiff(document, command.TableName);
					return false;
				});
				if (ribbonDiff == null)
					return CommandResult.Fail($"RibbonDiffXml was not found in the exported solution <{solutionName}>.");

				var xml = ribbonDiff.ToString();
				if (progress == null)
					output.WriteLine(xml);
				else
				{
					await File.WriteAllTextAsync(command.FileName, xml, cancellationToken);
					output.WriteLine($"RibbonDiffXml saved to {command.FileName}", ConsoleColor.Green);
				}
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
