using System.Xml.Linq;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class GetRibbonDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<GetRibbonDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetRibbonDiffCommand command, CancellationToken cancellationToken)
		{
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
				var validationError = await RibbonDiffExport.ValidateSolutionAsync(
					crm, sourceSolution.Id, solutionName, componentId, componentType, cancellationToken);
				if (validationError != null)
					return CommandResult.Fail(validationError);

				using var solution = await solutionRepository.CreateTemporarySolutionAsync(crm, sourceSolution.publisherid);
				await solution.AddComponentAsync(componentId, componentType, includeSubcomponents: false);
				using var archive = await solution.DownloadAsync();
				XElement? ribbonDiff = null;
				archive.UpdateEntryXml("customizations.xml", document =>
				{
					ribbonDiff = RibbonDiffExport.FindRibbonDiff(document, command.TableName);
					return false;
				});
				if (ribbonDiff == null)
					return CommandResult.Fail("RibbonDiffXml was not found in the exported temporary solution.");

				var xml = ribbonDiff.ToString();
				if (string.IsNullOrWhiteSpace(command.FileName))
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
