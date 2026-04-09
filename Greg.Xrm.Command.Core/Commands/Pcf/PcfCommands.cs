using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pcf
{
	[Command("pcf", "test", HelpText = "Run PCF component tests in headless mode for CI/CD.")]
	public class PcfTestCommand
	{
		[Option("path", "p", Order = 1, HelpText = "Path to the PCF project directory. Defaults to current directory.")]
		public string? Path { get; set; }

		[Option("browser", "b", Order = 2, DefaultValue = "headless", HelpText = "Browser mode: headless, chrome, firefox, edge.")]
		public string Browser { get; set; } = "headless";

		[Option("reporter", "r", Order = 3, DefaultValue = "spec", HelpText = "Test reporter: spec, json, junit.")]
		public string Reporter { get; set; } = "spec";
	}

	[Command("pcf", "publish", HelpText = "Publish a PCF component without full solution import.")]
	public class PcfPublishCommand
	{
		[Option("path", "p", Order = 1, HelpText = "Path to the PCF project directory.")]
		public string? Path { get; set; }

		[Option("solution", "s", Order = 2, HelpText = "Solution unique name to publish into.")]
		public string? SolutionUniqueName { get; set; }

		[Option("dry-run", Order = 3, HelpText = "Show what would be published without actually publishing.")]
		public bool DryRun { get; set; }
	}

	[Command("pcf", "version", "bump", HelpText = "Semantic version management for PCF components with changelog.")]
	public class PcfVersionBumpCommand
	{
		[Option("path", "p", Order = 1, HelpText = "Path to the PCF project directory.")]
		public string? Path { get; set; }

		[Option("type", "t", Order = 2, Required = true, HelpText = "Version bump type: major, minor, patch.")]
		public string BumpType { get; set; } = "";

		[Option("message", "m", Order = 3, HelpText = "Changelog message for the version bump.")]
		public string? Message { get; set; }
	}

	[Command("pcf", "dependency-check", HelpText = "Validate PCF dependencies are satisfied in target environment.")]
	public class PcfDependencyCheckCommand
	{
		[Option("path", "p", Order = 1, HelpText = "Path to the PCF project directory.")]
		public string? Path { get; set; }

		[Option("environment", "e", Order = 2, HelpText = "Target environment ID to check against.")]
		public string? EnvironmentId { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
