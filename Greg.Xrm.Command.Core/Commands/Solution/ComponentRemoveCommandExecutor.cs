using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentRemoveCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		ISolutionComponentRepository solutionComponentRepository
		) : ICommandExecutor<ComponentRemoveCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ComponentRemoveCommand command, CancellationToken cancellationToken)
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


			output.Write($"Retrieving solution component associated to object {command.ComponentId}...");
			var component = await solutionComponentRepository.GetBySolutionIdAndObjectIdAsync(crm, solution.Id, command.ComponentId);
			if (component == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Component with id '{command.ComponentId}' not found in solution '{solutionName}'.");
			}
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write($"Removing component {component} from solution {solutionName}...");
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

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}

		}
	}
}
