using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Data
{
	[Command("data", "export", HelpText = "Export data from Dataverse tables using pure .NET 8+ engine (cross-platform).")]
	public class DataExportCommand
	{
		[Option("tables", "t", Order = 1, Required = true, HelpText = "Comma-separated list of tables to export.")]
		public string[] Tables { get; set; } = Array.Empty<string>();

		[Option("output", "o", Order = 2, Required = true, HelpText = "Output directory for exported data.")]
		public string OutputPath { get; set; } = "";

		[Option("format", "f", Order = 3, DefaultValue = "json", HelpText = "Export format: json, csv, xml.")]
		public string Format { get; set; } = "json";

		[Option("solution", "s", Order = 4, HelpText = "Export all tables from a specific solution.")]
		public string? SolutionUniqueName { get; set; }

		[Option("include-relationships", Order = 5, HelpText = "Include relationship/lookup data in export.")]
		public bool IncludeRelationships { get; set; }

		[Option("batch-size", Order = 6, DefaultValue = 500, HelpText = "Number of records per page.")]
		public int BatchSize { get; set; } = 500;
	}

	[Command("data", "import", HelpText = "Import data into Dataverse tables using pure .NET 8+ engine (cross-platform).")]
	public class DataImportCommand
	{
		[Option("input", "i", Order = 1, Required = true, HelpText = "Path to data directory or file to import.")]
		public string InputPath { get; set; } = "";

		[Option("format", "f", Order = 2, DefaultValue = "json", HelpText = "Import format: json, csv, xml.")]
		public string Format { get; set; } = "json";

		[Option("table", "t", Order = 3, HelpText = "Target table name (if importing single table).")]
		public string? TargetTable { get; set; }

		[Option("mode", "m", Order = 4, DefaultValue = "upsert", HelpText = "Import mode: upsert, create-only, delete.")]
		public string Mode { get; set; } = "upsert";

		[Option("batch-size", Order = 5, DefaultValue = 500, HelpText = "Number of records per batch.")]
		public int BatchSize { get; set; } = 500;

		[Option("dry-run", Order = 6, HelpText = "Validate data without importing.")]
		public bool DryRun { get; set; }
	}
}
