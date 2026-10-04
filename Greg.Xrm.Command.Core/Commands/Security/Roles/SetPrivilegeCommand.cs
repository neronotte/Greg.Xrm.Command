using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "set-privilege", HelpText = "Set or remove a privilege on a security role, validating its supported access levels.")]
	[Alias("security", "role", "set-privilege")]
	[Alias("security", "role", "setPrivilege")]
	[Alias("security", "roles", "setPrivilege")]
	public class SetPrivilegeCommand : IValidatableObject, ICanProvideUsageExample
	{
		private string? level;

		[Option("role", "r", Order = 1, HelpText = "Exact root role name or role GUID. Use a GUID when the name is ambiguous.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		[Option("name", "n", Order = 10, HelpText = "Technical privilege name, for example prvWriteAccount. Alternative to --table and --privilege.")]
		public string? Name { get; set; }

		[Option("table", "t", Order = 11, HelpText = "Table name used to build the privilege name, for example Account. Requires --privilege.")]
		public string? Table { get; set; }

		[Option("privilege", "p", Order = 12, HelpText = "Privilege action, for example Write. Requires --table.")]
		public string? Privilege { get; set; }

		[Option("level", "l", Order = 2, HelpText = "Required: 1/Basic/User, 2/Local/BusinessUnit, 3/Deep/ParentChild, 4/Global/Organization. 0, an empty value or null removes the privilege.")]
		public string? Level
		{
			get => this.level;
			set
			{
				this.level = value;
				this.LevelSpecified = true;
			}
		}

		public bool LevelSpecified { get; private set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!this.LevelSpecified)
				yield return new ValidationResult("Option --level is required. Use 0, an empty value or null to remove the privilege.", [nameof(Level)]);

			var hasName = !string.IsNullOrWhiteSpace(this.Name);
			var hasTable = !string.IsNullOrWhiteSpace(this.Table);
			var hasPrivilege = !string.IsNullOrWhiteSpace(this.Privilege);
			if (hasName && (hasTable || hasPrivilege))
				yield return new ValidationResult("Use either --name or --table with --privilege, not both.", [nameof(Name), nameof(Table), nameof(Privilege)]);
			else if (!hasName && (!hasTable || !hasPrivilege))
				yield return new ValidationResult("Provide --name or both --table and --privilege.", [nameof(Name), nameof(Table), nameof(Privilege)]);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Only unmanaged security roles can be modified. Managed roles are rejected, including when removing a privilege.");
			writer.WriteTitle3("Privilege levels");
			writer.WriteParagraph("Pass --level (or -l) followed by a number from 0 to 4 or one of the accepted names below. Names are case-insensitive; surrounding whitespace is ignored.");
			string[][] levels =
			[
				["0", "None (remove)", "null or \"\"", "Remove the privilege from the role."],
				["1", "Basic", "Basic, User", "User-level access, including records owned by the user or their teams."],
				["2", "Local", "Local, BusinessUnit", "Access within the user's business unit."],
				["3", "Deep", "Deep, ParentChild", "Access within the user's business unit and its child business units."],
				["4", "Global", "Global, Organization", "Access throughout the organization."]
			];
			writer.WriteTable(levels, ["Number", "Level", "Accepted names", "Access scope"], row => row).WriteLine();
			writer.WriteParagraph("Not every level is available for every privilege. The command checks the specific privilege in Dataverse and rejects unsupported levels before making changes. Values outside 0-4, including 5, are not accepted. These numbers follow RoleEditor levels, not the underlying SDK enum values.");
			writer.WriteParagraph("The --level option must be provided. Use -l 0, -l null or -l \"\" to remove a privilege; omitting --level is an error. If your shell does not forward empty arguments, prefer -l 0 or -l null. You can also use security roles clear-privilege without a level option.");

			writer.WriteParagraph("Set a privilege by its technical name:");
			writer.WriteCodeBlock("pacx security roles set-privilege -r \"Salesperson\" -n prvWriteAccount -l Basic", "Powershell");
			writer.WriteParagraph("Set a table privilege:");
			writer.WriteCodeBlock("pacx security roles set-privilege -r \"Salesperson\" -t Account -p Write -l Organization", "Powershell");
			writer.WriteParagraph("Set Local (BusinessUnit) access using its numeric level:");
			writer.WriteCodeBlock("pacx security roles set-privilege -r \"Salesperson\" -t Account -p Write -l 2", "Powershell");
			writer.WriteParagraph("Remove a privilege:");
			writer.WriteCodeBlock("pacx security roles set-privilege -r \"Salesperson\" -n prvWriteAccount -l null", "Powershell");
			writer.WriteParagraph("Remove a privilege using numeric level 0:");
			writer.WriteCodeBlock("pacx security roles set-privilege -r \"Salesperson\" -n prvWriteAccount -l 0", "Powershell");
		}
	}
}