using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Dlp
{
	[Command("dlp", "policy-audit", HelpText = "Review and report on DLP policy coverage across connectors and environments.")]
	public class DlpPolicyAuditCommand
	{
		[Option("environment", "e", Order = 1, HelpText = "Filter by environment ID.")]
		public string? EnvironmentId { get; set; }

		[Option("connector", "c", Order = 2, HelpText = "Filter by connector ID or name.")]
		public string? ConnectorId { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("show-gaps", Order = 4, HelpText = "Show connectors without DLP policies.")]
		public bool ShowGaps { get; set; }
	}
}
