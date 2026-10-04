using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[Command("security", "teams", "get", "roles", HelpText = "List the security roles assigned to a team.")]
	[Alias("security", "team", "get", "roles")]
	[Alias("security", "teams", "get-roles")]
	[Alias("security", "team", "get-roles")]
	[Alias("security", "teams", "getRoles")]
	[Alias("security", "team", "getRoles")]
	public class GetRolesCommand : ICanProvideUsageExample
	{
		[Option("team", "t", Order = 1, HelpText = "Required team GUID or exact name. Use a GUID when names are ambiguous.")]
		[Required]
		public string Team { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx security teams get roles --team \"Sales\"", "Powershell");
			writer.WriteCodeBlock("pacx security team get-roles -t 00000000-0000-0000-0000-000000000001", "Powershell");
		}
	}
}