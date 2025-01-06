using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;

namespace Greg.Xrm.Command.Commands.Auth
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPacxProjectRepository pacxProjectRepository) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			var connections = await organizationServiceRepository.GetAllConnectionDefinitionsAsync();
			if (connections.Count == 0)
			{
				return CommandResult.Fail("No authentication profiles found.");
			}

			var project = await pacxProjectRepository.GetCurrentProjectAsync();


			output.WriteLine("The following authentication profiles are stored on this computer:");

			var padding = connections.ConnectionStringKeys.Max(x => x.Length) + 4;

			bool defaultFound = false, projectFound = false;
			foreach (var item in connections.ConnectionStringKeys)
			{
				var name = item;
				if (name.Equals(connections.CurrentConnectionStringKey, StringComparison.InvariantCultureIgnoreCase))
				{
					name += "*";
					defaultFound = true;
				}
				if (name.Equals(project?.AuthProfileName, StringComparison.InvariantCultureIgnoreCase))
				{
					name += "+";
					projectFound = true;
				}
				name = name.PadRight(padding);

				output.Write("  ");
				output.Write(name);


				var environmentName = await organizationServiceRepository.GetEnvironmentFromConnectioStringAsync(item);
				output.WriteLine(environmentName);
			}

			if (defaultFound || projectFound) output.WriteLine();
			if (defaultFound) output.WriteLine("* identifies the default authentication profile.");
			if (projectFound) output.WriteLine("+ identifies the authentication profile used by the current project.");

			return CommandResult.Success();
		}
	}
}
