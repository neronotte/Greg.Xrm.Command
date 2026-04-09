using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresource", "map", HelpText = "Map local files to Dataverse web resources with flexible file-to-resource mapping.")]
	public class WebResourceMapCommand
	{
		[Option("config", "c", Order = 1, Required = true, HelpText = "Path to the web resource mapping config file (YAML or JSON).")]
		public string ConfigPath { get; set; } = "";

		[Option("solution", "s", Order = 2, HelpText = "Solution unique name to publish web resources into.")]
		public string? SolutionUniqueName { get; set; }

		[Option("dry-run", Order = 3, HelpText = "Show what would be mapped/updated without actually uploading.")]
		public bool DryRun { get; set; }

		[Option("publish", "p", Order = 4, HelpText = "Publish web resources after uploading.")]
		public bool Publish { get; set; }

		[Option("force", "f", Order = 5, HelpText = "Force overwrite of existing web resources without prompting.")]
		public bool Force { get; set; }
	}
}
