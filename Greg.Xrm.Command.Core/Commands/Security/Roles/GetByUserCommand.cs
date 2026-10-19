using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "get-by-user", HelpText = "List the roles assigned to a user directly and through teams.")]
	[Alias("security", "role", "get-by-user")]
	[Alias("security", "roles", "getByUser")]
	[Alias("security", "role", "getByUser")]
	public class GetByUserCommand : ICanProvideUsageExample
	{
		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Show all the roles of the current user (the one used by the active connection):");
			writer.WriteCodeBlock("pacx security roles get-by-user", "Powershell");

			writer.WriteParagraph("Show all the roles of a specific user, both the ones assigned directly and the ones inherited through team membership. The user can be identified by id, domain name or primary email:");
			writer.WriteCodeBlock("pacx security roles get-by-user --user 00000000-0000-0000-0000-000000000000", "Powershell");
			writer.WriteCodeBlock("pacx security roles get-by-user -u john.doe@contoso.com", "Powershell");

			writer.WriteParagraph("The output reports, for each role, whether it comes from a direct assignment (Direct) or from a team (Team). Useful to understand why a user has a given permission.");
			writer.WriteParagraph("Note: membership of Microsoft Entra ID group teams is synchronized only when the user accesses the environment, so roles inherited from those teams may not be listed for users that never logged in.");
		}

		[Option("user", "u", Order = 1, HelpText = "The user to inspect: system user id, domain name or primary email. If omitted, the current user (the one used by the active connection) is considered.")]
		public string? User { get; set; }
	}
}
