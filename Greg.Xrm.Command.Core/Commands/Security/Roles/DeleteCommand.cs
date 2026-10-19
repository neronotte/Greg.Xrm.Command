using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("roles", "delete", HelpText = "Delete an unmanaged security role. Managed roles cannot be deleted.")]
	[Alias("role", "delete")]
	[Alias("security", "roles", "delete")]
	[Alias("security", "role", "delete")]
	public class DeleteCommand : ICanProvideUsageExample
	{
		[Option("role", "r", Order = 1, HelpText = "Exact root role name or role GUID. Only unmanaged security roles can be deleted.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Delete an unmanaged role by its exact root role name:");
			writer.WriteCodeBlock("pacx roles delete --role \"Salesperson - Copy\"", "Powershell");
			writer.WriteParagraph("Use a GUID to select an exact role when names are ambiguous:");
			writer.WriteCodeBlock("pacx roles delete -r 00000000-0000-0000-0000-000000000001", "Powershell");
			writer.WriteParagraph("Only unmanaged roles can be deleted. Deletion is immediate, without a confirmation prompt, and can remove access granted to users and teams. Dataverse enforces dependencies and restrictions; the command does not remove dependencies automatically.");
		}
	}
}