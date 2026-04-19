using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentMoveCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		ISolutionComponentRepository solutionComponentRepository
		) : ICommandExecutor<ComponentMoveCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ComponentMoveCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var fromSolutionName = command.FromSolutionUniqueName;
			if (string.IsNullOrWhiteSpace(fromSolutionName))
			{
				fromSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(fromSolutionName))
			{
				return CommandResult.Fail("No solution specified and no default solution found.");
			}


			output.Write($"Retrieving solution {fromSolutionName}...");
			var fromSolution = await solutionRepository.GetByUniqueNameAsync(crm, fromSolutionName);
			if (fromSolution == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Solution with unique name '{fromSolutionName}' not found.");
			}
			output.WriteLine("Done", ConsoleColor.Green);



			var toSolutionName = command.ToSolutionUniqueName;
			if (string.IsNullOrWhiteSpace(toSolutionName))
			{
				toSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(toSolutionName))
			{
				return CommandResult.Fail("No solution specified and no default solution found.");
			}


			output.Write($"Retrieving solution {toSolutionName}...");
			var toSolution = await solutionRepository.GetByUniqueNameAsync(crm, toSolutionName);
			if (toSolution == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Solution with unique name '{toSolutionName}' not found.");
			}
			output.WriteLine("Done", ConsoleColor.Green);
			if (toSolution.ismanaged)
			{
				return CommandResult.Fail($"Solution '{toSolutionName}' is managed. Cannot add components to a managed solution.");
			}

			if (fromSolution.Id == toSolution.Id)
			{
				return CommandResult.Fail("Source and target solutions are the same. No action needed.");
			}







			output.Write($"Retrieving solution component associated to object {command.ComponentId} from solution {fromSolutionName}...");
			var component = await solutionComponentRepository.GetBySolutionIdAndObjectIdAsync(crm, fromSolution.Id, command.ComponentId);
			if (component == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Component with id '{command.ComponentId}' not found in solution '{fromSolutionName}'.");
			}
			output.WriteLine("Done", ConsoleColor.Green);


			output.Write($"Moving component from solution {fromSolutionName} to solution {toSolutionName}...");
			try
			{
				var addRequest = new AddSolutionComponentRequest
				{
					SolutionUniqueName = toSolutionName,
					ComponentType = component.componenttype.Value,
					ComponentId = component.objectid,
				};

				if (component.componenttype.Value == (int)ComponentType.Entity)
				{
					addRequest.AddRequiredComponents = command.AddRequiredComponents;
					addRequest.DoNotIncludeSubcomponents = !command.IncludeSubcomponents;
				}

				var removeRequest = new RemoveSolutionComponentRequest
				{
					SolutionUniqueName = fromSolutionName,
					ComponentType = component.componenttype.Value,
					ComponentId = component.objectid // if you check the documentation, componentid must be the id of the component, but in reality it is the objectid field of the solution component record, which is the id of the component in the solution, not the id of the component itself. This is a bit confusing, but it is what it is.
				};


				var request = new ExecuteTransactionRequest
				{
					Requests = [
						addRequest,
						removeRequest
					]
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
