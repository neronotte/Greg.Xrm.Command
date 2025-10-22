
using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.ComponentResolution;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentListCommandExecutor(
	IOutput output,
	IOrganizationServiceRepository organizationServiceRepository,
	ISolutionRepository solutionRepository,
	ISolutionComponentRepository solutionComponentRepository,
	IComponentResolverEngine componentResolverEngine)
	: ICommandExecutor<ComponentListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ComponentListCommand command, CancellationToken cancellationToken)
		{

			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}



			Greg.Xrm.Command.Model.Solution? solution = null;
			try
			{
				output.Write($"Retrieving solution {currentSolutionName}...");

				solution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
				if (solution == null)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Solution {currentSolutionName} not found.");
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error while retrieving solution: " + ex.Message, ex);
			}




			List<Command.Model.SolutionComponent> solutionComponents;
			try
			{
				output.Write($"Retrieving components for solution {currentSolutionName}...");

				solutionComponents = await solutionComponentRepository.GetBySolutionIdAsync(crm, solution.Id);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error while retrieving solution components: " + ex.Message, ex);
			}


			if (solutionComponents.Count == 0)
			{
				output.WriteLine("No components found in the solution.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}


			try
			{
				output.Write($"Resolving components for solution {currentSolutionName}...");
				await componentResolverEngine.ResolveAllAsync(solutionComponents, crm);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error while resolving components: " + ex.Message, ex);
			}

			solutionComponents = solutionComponents
				.OrderBy(x => x.TypeLabel)
				.ThenBy(x => x.Label)
				.ToList();

			if (command.Format == ComponentListCommand.OutputFormat.Table)
			{
				PrintTableCompact(solutionComponents);
			}
			else
			{
				PrintJson(solutionComponents);
			}


				return CommandResult.Success();
		}

		private void PrintTableCompact(List<SolutionComponent> solutionComponents)
		{
			output.WriteLine();
			output.WriteTable(
				solutionComponents,
				rowHeaders: () => ["TypeCode", "Type", "Object ID", "Label"],
				rowData: x => [
					x.componenttype?.Value.ToString()?? string.Empty, 
					x.TypeLabel, 
					x.objectid.ToString(), 
					x.Label]
			);
		}

		private void PrintJson(List<SolutionComponent> solutionComponents)
		{
			output.WriteLine();

			var items = solutionComponents.Select(SolutionComponentDto.FromEntity).ToList();

			var text = JsonConvert.SerializeObject(items, Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});

			output.WriteLine(text);
		}
	}
}
