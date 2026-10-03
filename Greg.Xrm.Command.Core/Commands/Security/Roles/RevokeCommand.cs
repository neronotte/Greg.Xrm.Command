using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "revoke", HelpText = "Revoke a direct security role assignment from a user, a team, or both, skipping absent assignments.")]
	[Alias("security", "roles", "remove")]
	[Alias("security", "roles", "disassociate")]
	[Alias("security", "role", "revoke")]
	[Alias("security", "role", "remove")]
	[Alias("security", "role", "disassociate")]
	public class RevokeCommand : RoleAssignmentCommand
	{
		public override void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Revoke a direct role assignment. Absent assignments are left unchanged; roles inherited through team membership are not revoked.");
			writer.WriteCodeBlock("pacx security roles revoke --role \"Salesperson\" --user john.doe@contoso.com", "Powershell");
			writer.WriteParagraph("When record ownership across business units is enabled, --businessunit is required. Otherwise it is ignored and each recipient's business unit is used.");
			writer.WriteCodeBlock("pacx security roles revoke -r \"Salesperson\" -u john.doe@contoso.com -t \"Sales\" -bu \"Europe\"", "Powershell");
		}
	}
}