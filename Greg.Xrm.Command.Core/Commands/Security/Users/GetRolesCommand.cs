using Greg.Xrm.Command.Commands.Security.Roles;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[Command("users", "getRoles", HelpText = "List the roles assigned to a user directly and through teams. Equivalent to security roles get-by-user.")]
	[Alias("user", "getRoles")]
	[Alias("user", "roles")]
	[Alias("users", "get-roles")]
	[Alias("user", "get-roles")]
	[Alias("users", "getRole")]
	[Alias("user", "getRole")]
	[Alias("users", "get-role")]
	[Alias("user", "get-role")]
	[Alias("security", "users", "getRoles")]
	[Alias("security", "user", "getRoles")]
	[Alias("security", "users", "get-roles")]
	[Alias("security", "user", "get-roles")]
	[Alias("security", "users", "getRole")]
	[Alias("security", "user", "getRole")]
	[Alias("security", "users", "get-role")]
	[Alias("security", "user", "get-role")]
	public class GetRolesCommand : ICanProvideUsageExample
	{
		[Option("user", "u", Order = 1, HelpText = "The user to inspect: system user id, domain name or primary email. If omitted, the current user (the one used by the active connection) is considered.")]
		public string? User { get; set; }

		internal GetByUserCommand ToGetByUserCommand() => new() { User = this.User };

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Equivalent to pacx security roles get-by-user: list roles assigned directly and inherited through team membership, with their assignment sources.");
			writer.WriteParagraph("Show the roles of the current connected user:");
			writer.WriteCodeBlock("pacx users getRoles", "Powershell");
			writer.WriteParagraph("Select a user by GUID, domain name or primary email:");
			writer.WriteCodeBlock("pacx user get-roles --user john.doe@contoso.com", "Powershell");
			writer.WriteCodeBlock("pacx security users get-roles -u john.doe@contoso.com", "Powershell");
			writer.WriteParagraph("Microsoft Entra group-team membership is synchronized when the user accesses the environment, so inherited roles may not be listed for users who have never logged in.");
		}
	}
}