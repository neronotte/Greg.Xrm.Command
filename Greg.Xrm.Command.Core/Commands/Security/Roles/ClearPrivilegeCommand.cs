using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "clear-privilege", HelpText = "Remove a privilege from a security role. Equivalent to set-privilege with --level null.")]
	[Alias("security", "role", "clear-privilege")]
	[Alias("security", "roles", "clearPrivilege")]
	[Alias("security", "role", "clearPrivilege")]
	public class ClearPrivilegeCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("role", "r", Order = 1, HelpText = "Exact root role name or role GUID. Use a GUID when the name is ambiguous.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		[Option("name", "n", Order = 10, HelpText = "Technical privilege name, for example prvWriteAccount. Alternative to --table and --privilege.")]
		public string? Name { get; set; }

		[Option("table", "t", Order = 11, HelpText = "Table name used to build the privilege name, for example Account. Requires --privilege.")]
		public string? Table { get; set; }

		[Option("privilege", "p", Order = 12, HelpText = "Privilege action, for example Write. Requires --table.")]
		public string? Privilege { get; set; }

		internal SetPrivilegeCommand ToSetPrivilegeCommand() => new()
		{
			Role = this.Role,
			Name = this.Name,
			Table = this.Table,
			Privilege = this.Privilege,
			Level = null
		};

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			return this.ToSetPrivilegeCommand().Validate(validationContext);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Remove a privilege by its technical name:");
			writer.WriteCodeBlock("pacx security roles clear-privilege -r \"Salesperson\" -n prvWriteAccount", "Powershell");
			writer.WriteParagraph("Remove a table privilege:");
			writer.WriteCodeBlock("pacx security roles clear-privilege -r \"Salesperson\" -t Account -p Write", "Powershell");
		}
	}
}