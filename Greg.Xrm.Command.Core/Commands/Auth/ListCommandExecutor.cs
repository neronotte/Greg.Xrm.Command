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
			var overrideName = await organizationServiceRepository.GetCurrentEnvironmentOverrideNameAsync();

			output.WriteLine("The following authentication profiles are stored on this computer:");

			var padding = connections.ConnectionStringKeys.Max(x => x.Length) + 4;

			bool defaultFound = false, projectFound = false, overrideFound = false;
			foreach (var item in connections.ConnectionStringKeys)
			{
				var isDefault  = item.Equals(connections.CurrentConnectionStringKey, StringComparison.InvariantCultureIgnoreCase);
				var isProject  = item.Equals(project?.AuthProfileName, StringComparison.InvariantCultureIgnoreCase);
				var isOverride = overrideName != null && item.Equals(overrideName, StringComparison.OrdinalIgnoreCase);

				var name = item;
				if (isDefault)  { name += "*"; defaultFound  = true; }
				if (isProject)  { name += "+"; projectFound  = true; }
				if (isOverride) { name += "!"; overrideFound = true; }
				name = name.PadRight(padding);

				// Override > default > project > plain
				ConsoleColor? rowColor = isOverride ? ConsoleColor.DarkYellow
									   : isDefault  ? ConsoleColor.Cyan
									   : isProject  ? ConsoleColor.Green
									   : null;

				var environmentName = await organizationServiceRepository.GetEnvironmentFromConnectioStringAsync(item);

				output.Write("  ");
				if (rowColor.HasValue)
				{
					output.Write(name, rowColor.Value);
					output.WriteLine(environmentName, rowColor.Value);
				}
				else
				{
					output.Write(name);
					output.WriteLine(environmentName);
				}
			}

			if (defaultFound || projectFound || overrideFound) output.WriteLine();
			if (defaultFound)  output.WriteLine("* identifies the global default authentication profile.", ConsoleColor.Cyan);
			if (projectFound)  output.WriteLine("+ identifies the authentication profile used by the current project.", ConsoleColor.Green);
			if (overrideFound) output.WriteLine("! identifies the authentication profile used for this command (--environment override).", ConsoleColor.DarkYellow);

			return CommandResult.Success();
		}
	}
}
