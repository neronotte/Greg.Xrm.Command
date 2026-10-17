using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public enum RolePrivilegeViewMode { Assigned, All, Unassigned }
	public enum RolePrivilegeOutputFormat { Compact, Number, Technical, Functional, JsonTechnical, JsonFunctional, JsonNumeric }

	[Command("security", "roles", "get-privileges", HelpText = "Display a security role's table and miscellaneous privileges.")]
	[Alias("security", "role", "get-privileges")]
	[Alias("security", "roles", "get")]
	[Alias("security", "role", "get")]
	[Alias("security", "roles", "getPrivileges")]
	[Alias("security", "role", "getPrivileges")]
	public class GetPrivilegesCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("role", "r", Order = 1, HelpText = "Exact root role name or GUID. Managed and unmanaged roles can be inspected.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		[Option("table", "t", Order = 10, HelpText = "Show tables whose logical, schema or display name contains this text (LIKE contains, case-insensitive). Hides miscellaneous privileges.")]
		public string? Table { get; set; }

		[Option("privilege", "p", Order = 11, HelpText = "Filter by technical privilege name or table action name (contains, case-insensitive).")]
		public string? Privilege { get; set; }

		[Option("show", "s", Order = 20, DefaultValue = RolePrivilegeViewMode.Assigned, HelpText = "Assigned: tables with any granted privilege; All: all tables; Unassigned: tables with none. Also filters miscellaneous assignments.")]
		public RolePrivilegeViewMode Mode { get; set; } = RolePrivilegeViewMode.Assigned;

		[Option("format", "f", Order = 21, HelpText = "c/compact, n/number, t/tech/technical, f/func/functional; JSON: jt/jsontech/jsontechnical, jf/json/jsonfunc/jsonfunctional, jn/jsonnumeric. Default: functional.")]
		public string? Format { get; set; } = "functional";

		public RolePrivilegeOutputFormat? OutputFormat => this.Format?.Trim().ToLowerInvariant() switch
		{
			"c" or "compact" => RolePrivilegeOutputFormat.Compact,
			"n" or "number" => RolePrivilegeOutputFormat.Number,
			"t" or "tech" or "technical" => RolePrivilegeOutputFormat.Technical,
			"f" or "func" or "functional" => RolePrivilegeOutputFormat.Functional,
			"jsontech" or "jsontechnical" or "jt" => RolePrivilegeOutputFormat.JsonTechnical,
			"json" or "jsonfunc" or "jsonfunctional" or "jf" => RolePrivilegeOutputFormat.JsonFunctional,
			"jsonnumeric" or "jn" => RolePrivilegeOutputFormat.JsonNumeric,
			_ => null
		};

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!Enum.IsDefined(this.Mode))
				yield return new ValidationResult("Show must be Assigned, All or Unassigned.", [nameof(Mode)]);
			if (!this.OutputFormat.HasValue)
				yield return new ValidationResult("Format must be c/compact, n/number, t/tech/technical, f/func/functional, jt/jsontech/jsontechnical, jf/json/jsonfunc/jsonfunctional or jn/jsonnumeric.", [nameof(Format)]);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Inspect a role's assigned privileges (functional format is the default):");
			writer.WriteCodeBlock("pacx security roles get-privileges -r \"Salesperson\"", "Powershell");
			writer.WriteParagraph("Filter tables by a case-insensitive LIKE contains search. claim includes new_claims and new_claimresponse. A table filter suppresses miscellaneous privileges:");
			writer.WriteCodeBlock("pacx security roles get -r \"Salesperson\" -t claim --show All -f c", "Powershell");
			writer.WriteParagraph("Show tables with NO assigned privilege, plus unassigned miscellaneous privileges:");
			writer.WriteCodeBlock("pacx security roles getPrivileges -r \"Salesperson\" -s Unassigned", "Powershell");
			writer.WriteParagraph("Filter by action or technical privilege name:");
			writer.WriteCodeBlock("pacx security roles get-privileges -r \"Salesperson\" -p Read -f number", "Powershell");
			string[][] formats =
			[
				["c, compact", "CRWDATaS", "0, 1, 2, 3, 4"],
				["n, number", "Standard grid", "0, 1, 2, 3, 4"],
				["t, tech, technical", "Standard grid", "None, Basic, Local, Deep, Global"],
				["f, func, functional (default)", "Standard grid", "None, User, Business Unit, Parent Child, Organization"],
				["jt, jsontech, jsontechnical", "JSON object", "None, Basic, Local, Deep, Global"],
				["jf, json, jsonfunc, jsonfunctional", "JSON object", "None, User, Business Unit, Parent Child, Organization"],
				["jn, jsonnumeric", "JSON object", "0, 1, 2, 3, 4 (JSON numbers)"]
			];
			writer.WriteTable(formats, ["--format / -f", "Layout", "Levels"], row => row).WriteLine();
			writer.WriteParagraph("JSON formats emit an object keyed by technical privilege names, with no command progress, role heading or result summary. Shared privileges appear once; unsupported or filtered-out actions are omitted. Table filters also suppress miscellaneous entries in JSON. Unknown depths are null.");
			writer.WriteCodeBlock("pacx security roles get-privileges -r \"Salesperson\" --show All -f json", "Powershell");
			writer.WriteParagraph("Compact positions are Create, Read, Write, Delete, Append, Append To, Assign, Share (CRWDATaS). A blank is an unsupported action; 0/None is an available but unassigned privilege; - is excluded by the privilege filter. Numeric depths follow RoleEditor, not the SDK enum. Format names are case-insensitive. The obsolete --compact flag is not accepted.");
		}
	}
}