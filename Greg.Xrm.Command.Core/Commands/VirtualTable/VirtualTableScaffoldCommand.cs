using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.VirtualTable
{
	[Command("virtual-table", "scaffold", HelpText = "Scaffold virtual table definitions from external data sources.")]
	public class VirtualTableScaffoldCommand
	{
		[Option("datasource", "d", Order = 1, HelpText = "External data source type: SqlServer, OData, SharePoint.")]
		[Required]
		public string DataSourceType { get; set; } = "";

		[Option("connection", "c", Order = 2, HelpText = "Connection string or connection name.")]
		[Required]
		public string ConnectionString { get; set; } = "";

		[Option("tables", "t", Order = 3, HelpText = "Comma-separated list of external table names to scaffold.")]
		public string[]? ExternalTables { get; set; }

		[Option("prefix", "p", Order = 4, HelpText = "Logical name prefix for virtual tables. Defaults to data source type.")]
		public string? Prefix { get; set; }

		[Option("solution", "s", Order = 5, HelpText = "Solution unique name to add virtual tables to.")]
		public string? SolutionUniqueName { get; set; }

		[Option("dry-run", Order = 6, HelpText = "Show what would be created without actually creating.")]
		public bool DryRun { get; set; }

		[Option("format", "f", Order = 7, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
