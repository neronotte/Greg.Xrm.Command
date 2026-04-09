using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Storage
{
	[Command("storage", "analytics", HelpText = "Table-by-table storage analysis with cleanup recommendations.")]
	public class StorageAnalyticsCommand
	{
		[Option("top", Order = 1, DefaultValue = 20, HelpText = "Show top N tables by storage usage. Default is 20.")]
		public int TopN { get; set; } = 20;

		[Option("format", "f", Order = 2, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("recommendations", "r", Order = 3, HelpText = "Include cleanup recommendations.")]
		public bool IncludeRecommendations { get; set; }
	}
}
