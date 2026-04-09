using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Connector
{
	[Command("connector", "import", HelpText = "Import a custom connector from definition file.")]
	public class ConnectorImportCommand
	{
		[Option("file", "f", Order = 1, Required = true, HelpText = "Path to the connector definition JSON file.")]
		public string FilePath { get; set; } = "";

		[Option("solution", "s", Order = 2, HelpText = "Solution unique name to import into.")]
		public string? SolutionUniqueName { get; set; }

		[Option("dry-run", Order = 3, HelpText = "Validate definition without importing.")]
		public bool DryRun { get; set; }
	}

	[Command("connector", "export", HelpText = "Export a custom connector to definition file.")]
	public class ConnectorExportCommand
	{
		[Option("name", "n", Order = 1, Required = true, HelpText = "Custom connector unique name.")]
		public string ConnectorName { get; set; } = "";

		[Option("output", "o", Order = 2, Required = true, HelpText = "Output file path for connector definition JSON.")]
		public string OutputPath { get; set; } = "";
	}

	[Command("connector", "test", HelpText = "Test a custom connector with sample payloads.")]
	public class ConnectorTestCommand
	{
		[Option("name", "n", Order = 1, Required = true, HelpText = "Custom connector unique name.")]
		public string ConnectorName { get; set; } = "";

		[Option("operation", "o", Order = 2, Required = true, HelpText = "Operation/action name to test.")]
		public string OperationName { get; set; } = "";

		[Option("payload", "p", Order = 3, HelpText = "Path to JSON file with sample payload.")]
		public string? PayloadPath { get; set; }
	}

	[Command("connector", "validate", HelpText = "Validate a custom connector definition against OpenAPI schema.")]
	public class ConnectorValidateCommand
	{
		[Option("file", "f", Order = 1, Required = true, HelpText = "Path to the connector definition JSON file.")]
		public string FilePath { get; set; } = "";

		[Option("strict", Order = 2, HelpText = "Treat warnings as errors.")]
		public bool Strict { get; set; }
	}
}
