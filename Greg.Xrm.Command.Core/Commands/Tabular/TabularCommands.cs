using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Tabular
{
	[Command("tabular", "deploy", HelpText = "Deploy a .bim file to a Power BI semantic model (idempotent).")]
	public class TabularDeployCommand
	{
		[Option("bim", "b", Order = 1, Required = true, HelpText = "Path to the .bim file.")]
		public string BimFilePath { get; set; } = "";

		[Option("workspace", "w", Order = 2, Required = true, HelpText = "Power BI workspace name or ID.")]
		public string Workspace { get; set; } = "";

		[Option("dataset", "d", Order = 3, HelpText = "Dataset name. Creates new if not exists.")]
		public string? DatasetName { get; set; }

		[Option("mode", "m", Order = 4, DefaultValue = "auto", HelpText = "Connection mode: auto, xmla, rest. Auto-detects based on workspace tier.")]
		public string Mode { get; set; } = "auto";

		[Option("dry-run", Order = 5, HelpText = "Show what would be deployed without actually deploying.")]
		public bool DryRun { get; set; }
	}

	[Command("tabular", "diff", HelpText = "Compare local .bim against deployed Power BI semantic model.")]
	public class TabularDiffCommand
	{
		[Option("bim", "b", Order = 1, Required = true, HelpText = "Path to the local .bim file.")]
		public string BimFilePath { get; set; } = "";

		[Option("workspace", "w", Order = 2, Required = true, HelpText = "Power BI workspace name or ID.")]
		public string Workspace { get; set; } = "";

		[Option("dataset", "d", Order = 3, HelpText = "Dataset name.")]
		public string? DatasetName { get; set; }

		[Option("format", "f", Order = 4, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("tabular", "validate", HelpText = "Validate .bim for circular deps, invalid refs, best practices.")]
	public class TabularValidateCommand
	{
		[Option("bim", "b", Order = 1, Required = true, HelpText = "Path to the .bim file.")]
		public string BimFilePath { get; set; } = "";

		[Option("strict", Order = 2, HelpText = "Treat warnings as errors.")]
		public bool Strict { get; set; }
	}

	[Command("bim", "compare", HelpText = "Compare two .bim files and output structural differences.")]
	public class BimCompareCommand
	{
		[Option("file-a", "a", Order = 1, Required = true, HelpText = "First .bim file path.")]
		public string FileA { get; set; } = "";

		[Option("file-b", "b", Order = 2, Required = true, HelpText = "Second .bim file path.")]
		public string FileB { get; set; } = "";

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
