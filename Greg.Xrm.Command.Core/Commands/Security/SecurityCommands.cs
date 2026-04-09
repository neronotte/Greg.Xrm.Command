using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Security
{
	[Command("security", "audit-user", HelpText = "Full privilege audit for a user — what can they actually do?")]
	public class SecurityAuditUserCommand
	{
		[Option("user", "u", Order = 1, Required = true, HelpText = "User email, domain\\username, or systemuserid.")]
		public string UserIdentifier { get; set; } = "";

		[Option("format", "f", Order = 2, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("detail", "d", Order = 3, HelpText = "Detail level: summary (default), full (all privileges).")]
		public string DetailLevel { get; set; } = "summary";
	}

	[Command("security", "sharing-report", HelpText = "Who has access to a specific record, and why?")]
	public class SecuritySharingReportCommand
	{
		[Option("entity", "e", Order = 1, Required = true, HelpText = "Entity logical name (e.g., account, contact).")]
		public string EntityLogicalName { get; set; } = "";

		[Option("id", Order = 2, Required = true, HelpText = "Record GUID.")]
		public string RecordId { get; set; } = "";

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
