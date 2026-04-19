using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentAddCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository
		) : ICommandExecutor<ComponentAddCommand>
	{

		public async Task<CommandResult> ExecuteAsync(ComponentAddCommand command, CancellationToken cancellationToken)
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


			output.Write($"Adding component to solution {solutionName}...");
			try
			{
				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solutionName,
					ComponentType = (int)command.ComponentType,
					ComponentId = command.ComponentId,
				};

				if (command.ComponentType == ComponentType.Entity)
				{
					request.AddRequiredComponents = command.AddRequiredComponents;
					request.DoNotIncludeSubcomponents = !command.IncludeSubcomponents;
				}

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
