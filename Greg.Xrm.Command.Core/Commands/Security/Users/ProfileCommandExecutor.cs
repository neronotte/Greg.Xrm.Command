using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Newtonsoft.Json;
using Spectre.Console;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	public class ProfileCommandExecutor(
		IOutput output, 
		IOrganizationServiceRepository connections,
		ISecurityUserProfileService profiles, 
		IAnsiConsole console) : ICommandExecutor<ProfileCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ProfileCommand command, CancellationToken cancellationToken)
		{
			try
			{
				
				var json = command.Format == ProfileOutputFormat.Json;

				if (!json) output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				if (!json) output.WriteLine("Done", ConsoleColor.Green);
				
				var profile = await profiles.GetAsync(crm, command.User, cancellationToken);
				if (json)
				{
					output.WriteLine(JsonConvert.SerializeObject(profile, Formatting.Indented));
					return CommandResult.Success();
				}
				
				var tree = new Tree(Markup.Escape($"{profile.FullName} ({profile.UserId})"));
				tree.AddNode(Markup.Escape($"Domain: {profile.DomainName}"));
				tree.AddNode(Markup.Escape(profile.BusinessUnit == null ? "Business unit: not available"
					: $"Business unit: {profile.BusinessUnit.Name} ({profile.BusinessUnit.Id})"));
				
				var roleNode = tree.AddNode($"Roles ({profile.Roles.Count})");
				
				foreach (var assignment in profile.Roles)
					roleNode.AddNode(Markup.Escape($"{RoleLabel(assignment.Role)} | Sources: {string.Join(", ", assignment.Sources)}"));
				
				if (profile.Roles.Count == 0) roleNode.AddNode("No roles");
				
				var teamNode = tree.AddNode($"Teams ({profile.Teams.Count})");
				foreach (var team in profile.Teams)
				{
					var node = teamNode.AddNode(Markup.Escape($"{team.Name} ({team.TeamId}) | {team.Type} | BU: {team.BusinessUnit}"));
					var assigned = node.AddNode($"Roles ({team.Roles.Count})");
					foreach (var role in team.Roles) assigned.AddNode(Markup.Escape(RoleLabel(role)));
					if (team.Roles.Count == 0) assigned.AddNode("No roles");
				}
				
				if (profile.Teams.Count == 0) teamNode.AddNode("No teams");
				
				console.Write(tree);
				
				var result = CommandResult.Success();
				result["UserId"] = profile.UserId;
				result["RoleCount"] = profile.Roles.Count;
				result["TeamCount"] = profile.Teams.Count;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}

		private static string RoleLabel(SecurityRoleInfo role) =>
			$"{role.Name} ({role.RoleId}) | BU: {role.BusinessUnit} | {(role.IsManaged ? "Managed" : "Unmanaged")}";
	}
}