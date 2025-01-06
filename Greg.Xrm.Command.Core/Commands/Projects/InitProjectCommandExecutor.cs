using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.Projects
{
    public class InitProjectCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IPacxProjectRepository pacxProjectRepository
	) : ICommandExecutor<InitProjectCommand>
	{
		public async Task<CommandResult> ExecuteAsync(InitProjectCommand command, CancellationToken cancellationToken)
		{
			var connectionSettings = await organizationServiceRepository.GetAllConnectionDefinitionsAsync();
			string? authProfileName = null;
			if (string.IsNullOrWhiteSpace(command.AuthenticationProfileName))
			{
				authProfileName = connectionSettings.CurrentConnectionStringKey;
				if (string.IsNullOrWhiteSpace(authProfileName))
				{
					return CommandResult.Fail("No authentication profile is currently selected. Use the --conn option to specify one.");
				}
			}
			else
			{
				authProfileName = connectionSettings.ConnectionStringKeys
					.FirstOrDefault(x => x.Equals(command.AuthenticationProfileName, StringComparison.OrdinalIgnoreCase));

				if (string.IsNullOrWhiteSpace(authProfileName))
				{
					return CommandResult.Fail("Invalid authentication profile name. Use pacx auth list to show the list of the auth profiles currently configured.");
				}
			}



			string? solutionName = null;
			if (string.IsNullOrWhiteSpace(command.SolutionName))
			{
				solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					return CommandResult.Fail("No default solution is currently selected. Use the --solution option to specify one.");
				}
			}
			else
			{
				solutionName = command.SolutionName;
			}


			try
			{
				output.Write($"Connecting to dataverse environment {authProfileName}...");
				var crm = await organizationServiceRepository.GetConnectionByName(authProfileName);
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write($"Retrieving solution {solutionName}...");
				var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
				if (solution != null)
				{
					output.WriteLine("Done", ConsoleColor.Green);
				}
				else
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Solution {solutionName} not found on environment {authProfileName}.");
				}


				var project = new PacxProjectDefinition
				{
					AuthProfileName = authProfileName,
					SolutionName = solution.uniquename
				};
				await pacxProjectRepository.SaveAsync(project, Environment.CurrentDirectory, cancellationToken);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while initializing the project: " + ex.Message, ex);
			}
		}
	}
}
