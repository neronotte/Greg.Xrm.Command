using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "assign", HelpText = "Assign a security role directly to a user, a team, or both, skipping existing assignments.")]
	[Alias("security", "roles", "add")]
	[Alias("security", "roles", "associate")]
	[Alias("security", "role", "assign")]
	[Alias("security", "role", "add")]
	[Alias("security", "role", "associate")]
	public class AssignCommand : RoleAssignmentCommand
	{
		public override void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Assign a role directly to a user or team. Existing direct assignments are left unchanged; inherited team roles are not modified.");
			writer.WriteCodeBlock("pacx security roles assign --role \"Salesperson\" --user john.doe@contoso.com", "Powershell");
			writer.WriteParagraph("When record ownership across business units is enabled, --businessunit is required. Otherwise it is ignored and each recipient's business unit is used.");
			writer.WriteCodeBlock("pacx security roles assign -r \"Salesperson\" -u john.doe@contoso.com -t \"Sales\" -bu \"Europe\"", "Powershell");
		}
	}
}