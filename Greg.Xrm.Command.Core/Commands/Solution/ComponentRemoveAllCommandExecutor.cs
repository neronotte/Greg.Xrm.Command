using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentRemoveAllCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		ISolutionComponentRepository solutionComponentRepository
		) : ICommandExecutor<ComponentRemoveAllCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ComponentRemoveAllCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var solutionName = command.SolutionUniqueName;
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				return CommandResult.Fail("No solution specified and no default solution found.");
			}


			output.Write($"Retrieving solution {solutionName}...");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
			if (solution == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Solution with unique name '{solutionName}' not found.");
			}
			output.WriteLine("Done", ConsoleColor.Green);
			if (solution.ismanaged)
			{
				return CommandResult.Fail($"Solution '{solutionName}' is managed. Cannot add components to a managed solution.");
			}


			output.Write($"Retrieving solution components associated to solution {solution.uniquename}...");
			var components = await solutionComponentRepository.GetBySolutionIdAsync(crm, solution.Id);
			output.WriteLine("Done", ConsoleColor.Green);

			components = components.Where(c => c.componenttype?.Value == (int)command.ComponentType).ToList();
			if (components.Count == 0)
			{
				output.WriteLine("No components of the specified type found in the solution.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}



			var errorCount = 0;
			var i = 0;
			foreach (var component in components)
			{
				i++;
				output.Write($"Removing component {i}/{components.Count} ({component.Id}) from solution {solutionName}...");
				try
				{
					var request = new RemoveSolutionComponentRequest
					{
						SolutionUniqueName = solutionName,
						ComponentType = component.componenttype.Value,
						ComponentId = component.objectid // if you check the documentation, componentid must be the id of the component, but in reality it is the objectid field of the solution component record, which is the id of the component in the solution, not the id of the component itself. This is a bit confusing, but it is what it is.
					};

					await crm.ExecuteAsync(request, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex)
				{
					output.WriteLine("Failed: " + ex.Message, ConsoleColor.Red);
					errorCount++;
				}
			}

			if (errorCount == 0) return CommandResult.Success();
			if (errorCount < components.Count)
			{
				return CommandResult.Fail($"Removed {components.Count - errorCount} components, but failed to remove {errorCount} components. Check the output for more details.");
			}
			return CommandResult.Fail($"No component has been removed from the solution. Check the output for more details.");
		}
	}
}
