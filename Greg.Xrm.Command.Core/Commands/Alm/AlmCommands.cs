using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Alm
{
	[Command("alm", "pipeline", "create", HelpText = "Create a deployment pipeline stage from template.")]
	public class AlmPipelineCreateCommand
	{
		[Option("name", "n", Order = 1, Required = true, HelpText = "Pipeline name.")]
		public string Name { get; set; } = "";

		[Option("type", "t", Order = 2, DefaultValue = "Deployment", HelpText = "Pipeline type: Deployment, Validation.")]
		public string Type { get; set; } = "Deployment";

		[Option("source-env", Order = 3, HelpText = "Source environment ID.")]
		public string? SourceEnvironmentId { get; set; }

		[Option("target-env", Order = 4, HelpText = "Target environment ID.")]
		public string? TargetEnvironmentId { get; set; }
	}

	[Command("alm", "pipeline", "run", HelpText = "Trigger a pipeline stage (Validate -> Deploy -> Configure).")]
	public class AlmPipelineRunCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Pipeline ID to run.")]
		public string PipelineId { get; set; } = "";

		[Option("stage", "s", Order = 2, HelpText = "Stage to run: validate, deploy, configure.")]
		public string? Stage { get; set; }

		[Option("wait", Order = 3, HelpText = "Wait for pipeline completion.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 4, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("alm", "env-var", "sync", HelpText = "Sync environment variables across environments with value mapping.")]
	public class AlmEnvVarSyncCommand
	{
		[Option("source", "s", Order = 1, Required = true, HelpText = "Source environment ID.")]
		public string SourceEnvironmentId { get; set; } = "";

		[Option("target", "t", Order = 2, Required = true, HelpText = "Target environment ID.")]
		public string TargetEnvironmentId { get; set; } = "";

		[Option("mapping", "m", Order = 3, HelpText = "Path to YAML mapping file for value overrides.")]
		public string? MappingFile { get; set; }

		[Option("dry-run", Order = 4, HelpText = "Show what would be synced without applying.")]
		public bool DryRun { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("alm", "env", "diff", HelpText = "Compare two environments: tables, columns, solutions, env vars, connections.")]
	public class AlmEnvDiffCommand
	{
		[Option("env-a", Order = 1, Required = true, HelpText = "First environment ID.")]
		public string EnvA { get; set; } = "";

		[Option("env-b", Order = 2, Required = true, HelpText = "Second environment ID.")]
		public string EnvB { get; set; } = "";

		[Option("scope", Order = 3, HelpText = "Diff scope: solutions, tables, envvars, connections, all.")]
		public string Scope { get; set; } = "all";

		[Option("format", "f", Order = 4, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("solution", "layer", HelpText = "Manage solution layers — version pinning and dependency resolution.")]
	public class SolutionLayerCommand
	{
		[Option("solution", "s", Order = 1, Required = true, HelpText = "Solution unique name.")]
		public string SolutionUniqueName { get; set; } = "";

		[Option("show", Order = 2, HelpText = "Show solution layer information.")]
		public bool Show { get; set; }

		[Option("pin-version", Order = 3, HelpText = "Pin solution to specific version.")]
		public string? PinVersion { get; set; }

		[Option("check-deps", Order = 4, HelpText = "Check and resolve dependencies.")]
		public bool CheckDependencies { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
