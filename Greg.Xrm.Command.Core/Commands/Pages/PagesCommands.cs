using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Pages
{
	[Command("pages", "site", "publish", HelpText = "Publish a Power Pages site from local source.")]
	public class PagesSitePublishCommand
	{
		[Option("source", "s", Order = 1, Required = true, HelpText = "Path to the Power Pages site source directory.")]
		public string SourcePath { get; set; } = "";

		[Option("website", "w", Order = 2, HelpText = "Website record GUID or unique name.")]
		public string? WebsiteId { get; set; }

		[Option("dry-run", Order = 3, HelpText = "Show what would be published without actually publishing.")]
		public bool DryRun { get; set; }
	}

	[Command("pages", "webtemplate", "sync", HelpText = "Sync web templates, page templates, content snippets between environments.")]
	public class PagesWebTemplateSyncCommand
	{
		[Option("source", "s", Order = 1, Required = true, HelpText = "Source environment ID.")]
		public string SourceEnvironmentId { get; set; } = "";

		[Option("target", "t", Order = 2, Required = true, HelpText = "Target environment ID.")]
		public string TargetEnvironmentId { get; set; } = "";

		[Option("type", Order = 3, HelpText = "Sync type: webtemplate, pagetemplate, contentsnippet, all.")]
		public string SyncType { get; set; } = "all";

		[Option("dry-run", Order = 4, HelpText = "Show what would be synced without applying.")]
		public bool DryRun { get; set; }
	}

	[Command("pages", "liquid", "lint", HelpText = "Validate Liquid templates for errors before deployment.")]
	public class PagesLiquidLintCommand
	{
		[Option("file", "f", Order = 1, Required = true, HelpText = "Path to the Liquid template file or directory.")]
		public string FilePath { get; set; } = "";

		[Option("strict", Order = 2, HelpText = "Treat warnings as errors.")]
		public bool Strict { get; set; }
	}
}
