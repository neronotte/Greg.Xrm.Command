using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security
{
	[Command("security", "check-privilege", HelpText = "Check the privileges a user has on a table or on a specific record.")]
	[Alias("security", "checkPrivilege")]
	[Alias("security", "users", "check-privilege")]
	[Alias("security", "users", "checkPrivilege")]
	public class CheckPrivilegeCommand : ICanProvideUsageExample
	{
		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Check the privileges the current user (the one used by the active connection) has on a table:");
			writer.WriteCodeBlock("pacx security check-privilege --table account", "Powershell");

			writer.WriteParagraph("Check the privileges a user has on a table (table-level check, no record specified). For each privilege the widest depth granted by the user's roles (directly or through teams) is shown:");
			writer.WriteCodeBlock("pacx security check-privilege --user 00000000-0000-0000-0000-000000000000 --table account", "Powershell");

			writer.WriteParagraph("Check the access rights a user has on a specific record (Create, Read, Write, Delete, Append, Append To, Assign, Share). The user can be identified by id, domain name or primary email:");
			writer.WriteCodeBlock("pacx security check-privilege -u john.doe@contoso.com -t account -id 11111111-1111-1111-1111-111111111111", "Powershell");

			writer.WriteParagraph("Record-level checks also take into account ownership, shared access and team membership, so they can differ from the table-level result.");
		}

		[Option("user", "u", Order = 1, HelpText = "The user to inspect: system user id, domain name or primary email. If omitted, the current user (the one used by the active connection) is considered.")]
		public string? User { get; set; }

		[Required]
		[Option("table", "t", Order = 2, HelpText = "The logical name of the Dataverse table to inspect.")]
		public string TableName { get; set; } = string.Empty;

		[Option("id", "id", Order = 3, HelpText = "Optional record id. If omitted, table-level privileges are returned.")]
		public string? RecordId { get; set; }
	}
}
