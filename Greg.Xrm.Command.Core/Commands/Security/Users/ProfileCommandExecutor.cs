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
				cancellationToken.ThrowIfCancellationRequested();
				var json = command.Format == ProfileOutputFormat.Json;

				if (!json) output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				if (!json) output.WriteLine("Done", ConsoleColor.Green);
				
				var profile = await profiles.GetAsync(crm, command.User, cancellationToken);
				if (json)
				{
					output.WriteRawLine(JsonConvert.SerializeObject(profile, Formatting.Indented));
					return CommandResult.Success();
				}
				
				var tree = new Tree($"{Value(profile.FullName)} [Gray]({profile.UserId})[/]");
				tree.AddNode($"[SkyBlue2]Domain:[/] {Value(profile.DomainName)}");
				tree.AddNode(profile.BusinessUnit == null ? "[SkyBlue2]Business unit:[/] [Gray]not available[/]"
					: $"[SkyBlue2]Business unit:[/] {Value(profile.BusinessUnit.Name)} [Gray]({profile.BusinessUnit.Id})[/]");
				
				var roleNode = tree.AddNode($"[SkyBlue2]Roles[/] [Gray]({profile.Roles.Count})[/]");
				
				foreach (var assignment in profile.Roles)
					roleNode.AddNode(RoleLabel(assignment.Role));
				
				if (profile.Roles.Count == 0) roleNode.AddNode("[Gray]No roles[/]");
				
				var teamNode = tree.AddNode($"[SkyBlue2]Teams[/] [Gray]({profile.Teams.Count})[/]");
				foreach (var team in profile.Teams)
				{
					var node = teamNode.AddNode($"{Value(team.Name)} [Gray]({team.TeamId})[/] | {Value(team.Type)} | [SkyBlue2]BU:[/] {Value(team.BusinessUnit)}");
					var assigned = node.AddNode($"[SkyBlue2]Roles[/] [Gray]({team.Roles.Count})[/]");
					foreach (var role in team.Roles) assigned.AddNode(RoleLabel(role));
					if (team.Roles.Count == 0) assigned.AddNode("[Gray]No roles[/]");
				}
				
				if (profile.Teams.Count == 0) teamNode.AddNode("[Gray]No teams[/]");
				
				console.WriteLine();
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
			$"{Value(role.Name)} [Gray]({role.RoleId})[/] | [SkyBlue2]BU:[/] {Value(role.BusinessUnit)} | [Gray]{(role.IsManaged ? "Managed" : "Unmanaged")}[/]";

		private static string Value(string value) => $"[SandyBrown]{Markup.Escape(value)}[/]";
	}
}