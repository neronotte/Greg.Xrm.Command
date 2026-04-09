using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresource", "watch", HelpText = "Watch local files and sync changes to Dataverse web resources on file save.")]
	public class WebResourceWatchCommand
	{
		[Option("config", "c", Order = 1, Required = true, HelpText = "Path to the web resource mapping config file (YAML or JSON).")]
		public string ConfigPath { get; set; } = "";

		[Option("solution", "s", Order = 2, HelpText = "Solution unique name to publish web resources into.")]
		public string? SolutionUniqueName { get; set; }

		[Option("debounce", Order = 3, DefaultValue = 500, HelpText = "Debounce delay in milliseconds before syncing after file change. Default is 500ms.")]
		public int DebounceMs { get; set; } = 500;

		[Option("publish", "p", Order = 4, HelpText = "Publish web resources after each upload.")]
		public bool Publish { get; set; }

		[Option("poll", Order = 5, HelpText = "Use polling instead of FileSystemWatcher (fallback for network shares).")]
		public bool Poll { get; set; }

		[Option("poll-interval", Order = 6, DefaultValue = 2000, HelpText = "Polling interval in milliseconds. Default is 2000ms.")]
		public int PollIntervalMs { get; set; } = 2000;
	}
}
