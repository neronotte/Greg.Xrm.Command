using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public enum MemberPrivilegeInheritance
	{
		TeamOnly = 0,
		DirectUserAndTeam = 1
	}

	[Command("roles", "clone", HelpText = "Clone a security role and its privileges, optionally changing its name, description, business unit and member privilege inheritance.")]
	[Alias("roles", "copy")]
	[Alias("role", "clone")]
	[Alias("role", "copy")]
	[Alias("security", "roles", "clone")]
	[Alias("security", "roles", "copy")]
	[Alias("security", "role", "clone")]
	[Alias("security", "role", "copy")]
	public class CloneCommand : ICanProvideUsageExample
	{
		[Option("role", "r", Order = 1, HelpText = "Exact root role name or source role GUID. Managed roles can also be copied.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		[Option("name", "n", Order = 10, HelpText = "New role name. If omitted, uses '<source> - Copy', with an increasing numeric suffix for subsequent copies.")]
		[StringLength(100)]
		public string? Name { get; set; }

		[Option("description", "d", Order = 11, HelpText = "New description. If omitted, preserves the source description. Pass an empty value to clear it.")]
		[StringLength(2000)]
		public string? Description { get; set; }

		[Option("businessunit", "bu", Order = 12, HelpText = "Destination business unit GUID or exact name. Defaults to the source business unit, regardless of the record ownership across business units setting.")]
		public string? BusinessUnit { get; set; }

		[Option("inheritance", "i", Order = 13, HelpText = "TeamOnly (0) or DirectUserAndTeam (1). If omitted, preserves the source's member privilege inheritance.")]
		[EnumDataType(typeof(MemberPrivilegeInheritance))]
		public MemberPrivilegeInheritance? Inheritance { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Copy all privileges and their access levels, without copying user or team assignments:");
			writer.WriteCodeBlock("pacx roles clone --role \"Salesperson\"", "Powershell");
			writer.WriteParagraph("Override the name and description:");
			writer.WriteCodeBlock("pacx roles copy -r \"Salesperson\" -n \"Regional Sales\" -d \"Regional sales permissions\"", "Powershell");
			writer.WriteParagraph("Select a destination business unit, regardless of the record ownership across business units setting:");
			writer.WriteCodeBlock("pacx role clone -r \"Salesperson\" -bu \"Europe\" -i TeamOnly", "Powershell");
			writer.WriteParagraph("TeamOnly (0): Team privileges only. DirectUserAndTeam (1): Direct User (Basic) access level and Team privileges. Omitted description and inheritance are preserved from the source.");
			writer.WriteParagraph("Automatic names are '<source> - Copy', then '<source> - Copy 2', and so on, continuing above existing copies across the environment. Long source names are shortened to keep the generated name within 100 characters.");
		}
	}
}