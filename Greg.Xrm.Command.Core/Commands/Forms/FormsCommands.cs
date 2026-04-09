using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "list", HelpText = "List all Microsoft Forms with metadata (ID, title, status, response count, owner).")]
	public class FormsListCommand
	{
		[Option("tenant", "t", Order = 1, Required = true, HelpText = "Tenant ID or domain (e.g., contoso.onmicrosoft.com).")]
		public string TenantId { get; set; } = "";

		[Option("owner", "o", Order = 2, HelpText = "Owner user ID. If not provided, lists current user's forms.")]
		public string? OwnerId { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("token", Order = 4, HelpText = "OAuth2 access token. Reads from MSAL cache or environment if not provided.")]
		public string? Token { get; set; }
	}

	[Command("forms", "response", "count", HelpText = "Quick count of responses for monitoring/alerting.")]
	public class FormsResponseCountCommand
	{
		[Option("tenant", "t", Order = 1, Required = true, HelpText = "Tenant ID or domain.")]
		public string TenantId { get; set; } = "";

		[Option("form-id", "f", Order = 2, Required = true, HelpText = "Form ID to count responses for.")]
		public string FormId { get; set; } = "";

		[Option("owner", "o", Order = 3, HelpText = "Owner user ID.")]
		public string? OwnerId { get; set; }

		[Option("token", Order = 4, HelpText = "OAuth2 access token.")]
		public string? Token { get; set; }
	}

	[Command("forms", "responses", "export", HelpText = "Export responses to CSV, JSON, or SQL with paged retrieval.")]
	public class FormsResponsesExportCommand
	{
		[Option("tenant", "t", Order = 1, Required = true, HelpText = "Tenant ID or domain.")]
		public string TenantId { get; set; } = "";

		[Option("form-id", "f", Order = 2, Required = true, HelpText = "Form ID to export responses from.")]
		public string FormId { get; set; } = "";

		[Option("output", "o", Order = 3, Required = true, HelpText = "Output file path (CSV, JSON, or SQL).")]
		public string OutputPath { get; set; } = "";

		[Option("owner", Order = 4, HelpText = "Owner user ID.")]
		public string? OwnerId { get; set; }

		[Option("format", Order = 5, DefaultValue = "csv", HelpText = "Export format: csv, json, sql.")]
		public string Format { get; set; } = "csv";

		[Option("incremental", "i", Order = 6, HelpText = "Only export responses since last export.")]
		public bool Incremental { get; set; }

		[Option("token", Order = 7, HelpText = "OAuth2 access token.")]
		public string? Token { get; set; }
	}
}
