using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pages
{
	[Command("pages", "site", "config", "export", HelpText = "Export Power Pages portal configuration (auth, navigation, themes).")]
	public class PagesSiteConfigExportCommand
	{
		[Option("site", "s", Order = 1, Required = true, HelpText = "Power Pages site name or ID.")]
		public string SiteId { get; set; } = "";

		[Option("output", "o", Order = 2, Required = true, HelpText = "Output directory for exported configuration.")]
		public string OutputPath { get; set; } = "";

		[Option("scope", Order = 3, DefaultValue = "all", HelpText = "Export scope: all, auth, navigation, themes, snippets.")]
		public string Scope { get; set; } = "all";

		[Option("format", "f", Order = 4, DefaultValue = "json", HelpText = "Export format: json, xml.")]
		public string Format { get; set; } = "json";
	}

	[Command("pages", "site", "config", "import", HelpText = "Import Power Pages portal configuration with conflict resolution.")]
	public class PagesSiteConfigImportCommand
	{
		[Option("site", "s", Order = 1, Required = true, HelpText = "Power Pages site name or ID.")]
		public string SiteId { get; set; } = "";

		[Option("input", "i", Order = 2, Required = true, HelpText = "Path to exported configuration directory.")]
		public string InputPath { get; set; } = "";

		[Option("scope", Order = 3, DefaultValue = "all", HelpText = "Import scope: all, auth, navigation, themes, snippets.")]
		public string Scope { get; set; } = "all";

		[Option("force", "f", Order = 4, HelpText = "Overwrite existing configuration without prompting.")]
		public bool Force { get; set; }

		[Option("dry-run", Order = 5, HelpText = "Show what would be imported without applying.")]
		public bool DryRun { get; set; }
	}
}
