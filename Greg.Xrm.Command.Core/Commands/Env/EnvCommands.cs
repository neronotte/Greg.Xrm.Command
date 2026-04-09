using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Env
{
	[Command("env", "create", HelpText = "Create a new Power Platform environment (Developer, Sandbox, Production).")]
	public class EnvCreateCommand
	{
		[Option("name", "n", Order = 1, Required = true, HelpText = "Environment display name.")]
		public string Name { get; set; } = "";

		[Option("type", "t", Order = 2, DefaultValue = "Sandbox", HelpText = "Environment type: Developer, Sandbox, Production, Trial.")]
		public string Type { get; set; } = "Sandbox";

		[Option("region", "r", Order = 3, HelpText = "Geographic region (e.g., unitedstates, europe, asia).")]
		public string? Region { get; set; }

		[Option("currency", Order = 4, DefaultValue = "USD", HelpText = "Base currency code.")]
		public string Currency { get; set; } = "USD";

		[Option("language", Order = 5, DefaultValue = "en-US", HelpText = "Base language code.")]
		public string Language { get; set; } = "en-US";

		[Option("security-group", Order = 6, HelpText = "Azure AD security group ID for access control.")]
		public string? SecurityGroupId { get; set; }

		[Option("wait", Order = 7, HelpText = "Wait for environment provisioning to complete.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 8, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("env", "clone", HelpText = "Clone an environment — schema only, schema+data, or selective tables.")]
	public class EnvCloneCommand
	{
		[Option("source", "s", Order = 1, Required = true, HelpText = "Source environment ID.")]
		public string SourceEnvironmentId { get; set; } = "";

		[Option("name", "n", Order = 2, Required = true, HelpText = "New environment name.")]
		public string Name { get; set; } = "";

		[Option("mode", "m", Order = 3, DefaultValue = "schema-only", HelpText = "Clone mode: schema-only, schema-data, selective.")]
		public string Mode { get; set; } = "schema-only";

		[Option("tables", "t", Order = 4, HelpText = "Comma-separated list of tables for selective mode.")]
		public string[]? Tables { get; set; }

		[Option("wait", Order = 5, HelpText = "Wait for clone operation to complete.")]
		public bool Wait { get; set; }
	}

	[Command("env", "capacity", "report", HelpText = "Report database and file storage capacity across all environments.")]
	public class EnvCapacityReportCommand
	{
		[Option("environment", "e", Order = 1, HelpText = "Filter by environment ID.")]
		public string? EnvironmentId { get; set; }

		[Option("format", "f", Order = 2, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
