using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Tabular
{
	[Command("tabular", "translate", HelpText = "Manage and deploy multi-language translations for Power BI semantic model measures and columns.")]
	public class TabularTranslateCommand
	{
		[Option("model", "m", Order = 1, Required = true, HelpText = "Power BI dataset/model ID or name.")]
		public string ModelId { get; set; } = "";

		[Option("file", "f", Order = 2, Required = true, HelpText = "Path to translation file (.json or .bim with translations).")]
		public string TranslationFile { get; set; } = "";

		[Option("language", "l", Order = 3, Required = true, HelpText = "Target language code (e.g., en-US, fr-FR, ja-JP).")]
		public string LanguageCode { get; set; } = "";

		[Option("mode", Order = 4, DefaultValue = "deploy", HelpText = "Operation mode: deploy, export, diff.")]
		public string Mode { get; set; } = "deploy";

		[Option("workspace", "w", Order = 5, HelpText = "Power BI workspace ID.")]
		public string? WorkspaceId { get; set; }

		[Option("format", Order = 6, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("tabular", "role", "add-measures", HelpText = "Bulk-add measures to all security roles in a Power BI semantic model.")]
	public class TabularRoleAddMeasuresCommand
	{
		[Option("model", "m", Order = 1, Required = true, HelpText = "Power BI dataset/model ID or name.")]
		public string ModelId { get; set; } = "";

		[Option("measures", Order = 2, Required = true, HelpText = "Comma-separated list of measure names to add to all roles.")]
		public string[] Measures { get; set; } = Array.Empty<string>();

		[Option("workspace", "w", Order = 3, HelpText = "Power BI workspace ID.")]
		public string? WorkspaceId { get; set; }

		[Option("dry-run", Order = 4, HelpText = "Show what would be added without applying.")]
		public bool DryRun { get; set; }

		[Option("format", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("tabular", "perspective", "manage", HelpText = "Create, update, and manage Power BI semantic model perspectives via CLI.")]
	public class TabularPerspectiveManageCommand
	{
		[Option("model", "m", Order = 1, Required = true, HelpText = "Power BI dataset/model ID or name.")]
		public string ModelId { get; set; } = "";

		[Option("action", "a", Order = 2, Required = true, HelpText = "Action: create, delete, list, add-tables, remove-tables.")]
		public string Action { get; set; } = "";

		[Option("name", "n", Order = 3, HelpText = "Perspective name (required for create/delete/add/remove).")]
		public string? PerspectiveName { get; set; }

		[Option("tables", "t", Order = 4, HelpText = "Comma-separated table names (for add-tables/remove-tables).")]
		public string[]? Tables { get; set; }

		[Option("workspace", "w", Order = 5, HelpText = "Power BI workspace ID.")]
		public string? WorkspaceId { get; set; }

		[Option("format", Order = 6, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
