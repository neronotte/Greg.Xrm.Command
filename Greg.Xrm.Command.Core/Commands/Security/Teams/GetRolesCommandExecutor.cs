using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	public class GetRolesCommandExecutor(
		IOutput output, IOrganizationServiceRepository connections,
		ITeamRepository teams,
		ISecurityRoleService roles) : ICommandExecutor<GetRolesCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetRolesCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				var team = await teams.ResolveAsync(crm, command.Team.Trim(), cancellationToken);
				var byTeam = await roles.GetRolesByTeamsAsync(crm, [team.Id], cancellationToken);

				var assigned = byTeam.TryGetValue(team.Id, out var found) ? found : [];

				output.WriteLine($"Found {assigned.Count} roles for team '{team.name}' ({team.Id}).");

				if (assigned.Count > 0) output.WriteTable(assigned, () => ["Id", "Name", "Business Unit", "Type"], role =>
					[role.RoleId.ToString(), role.Name, role.BusinessUnit, role.IsManaged ? "Managed" : "Unmanaged"]);

				var result = CommandResult.Success();
				result["TeamId"] = team.Id;
				result["TeamName"] = team.name;
				result["Count"] = assigned.Count;
				result["Roles"] = string.Join(", ", assigned.Select(role => role.Name));
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}
	}
}