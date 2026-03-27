using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Commands.Solution.Service;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class GenerateLateBoundConstantsCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IConstantsGeneratorService constantsGeneratorService) : ICommandExecutor<GenerateLateBoundConstantsCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GenerateLateBoundConstantsCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve solution name
				var solutionName = command.Solution;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (solutionName == null)
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}

				// Ensure output folders exist (create if missing)
				if (!string.IsNullOrWhiteSpace(command.OutputCs))
					Directory.CreateDirectory(command.OutputCs);

				if (!string.IsNullOrWhiteSpace(command.OutputJs))
					Directory.CreateDirectory(command.OutputJs);

				var request = new ConstantsOutputRequest
				{
					SolutionName = solutionName,
					OutputCs = command.OutputCs,
					NamespaceCs = command.NamespaceCs,
					OutputJs = command.OutputJs,
					NamespaceJs = command.NamespaceJs,
					JsHeader = command.JsHeader,
					WithTypes = command.WithTypes,
					WithDescriptions = command.WithDescriptions
				};

				var (csFiles, jsFiles) = await constantsGeneratorService.GenerateAsync(crm, request, cancellationToken);

				output.WriteLine();
				if (csFiles > 0)
					output.WriteLine($"Generated {csFiles} C# file(s) in: ", ConsoleColor.White).Write(command.OutputCs, ConsoleColor.Yellow).WriteLine();
				if (jsFiles > 0)
					output.WriteLine($"Generated {jsFiles} JS file(s) in: ", ConsoleColor.White).Write(command.OutputJs, ConsoleColor.Yellow).WriteLine();

				var result = CommandResult.Success();
				result["CsFilesGenerated"] = csFiles;
				result["JsFilesGenerated"] = jsFiles;
				result["Solution"] = solutionName;
				return result;
			}
			catch (InvalidOperationException ex)
			{
				return CommandResult.Fail(ex.Message);
			}
			catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
