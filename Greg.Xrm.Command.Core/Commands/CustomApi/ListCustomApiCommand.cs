using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "list",
		HelpText = "Lists Dataverse Custom APIs in the current environment.")]
	public class ListCustomApiCommand : ICanProvideUsageExample
	{
		[Option("filter", "f", Order = 1,
			HelpText = "Case-insensitive substring match against unique name or display name.")]
		public string? Filter { get; set; }

		[Option("publisher", "pub", Order = 2,
			HelpText = "Filter by publisher prefix (e.g. nn).")]
		public string? Publisher { get; set; }

		[Option("type", "t", Order = 3,
			HelpText = "Filter by type: Action or Function.")]
		public CustomApiType? Type { get; set; }

		[Option("full", Order = 4,
			HelpText = "Also show the full signature of each Custom API (parameters and response properties).")]
		public bool Full { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("List all Custom APIs in the current environment:");
			writer.WriteCodeBlock("pacx customapi list", "Powershell");

			writer.WriteParagraph("Filter by publisher prefix to see only your organisation's APIs:");
			writer.WriteCodeBlock("pacx customapi list --publisher nn", "Powershell");

			writer.WriteParagraph("Find all Custom APIs whose name or display name contains 'Greg':");
			writer.WriteCodeBlock("pacx customapi list -f Greg", "Powershell");

			writer.WriteParagraph("List only Custom Functions (GET) for a specific publisher:");
			writer.WriteCodeBlock("pacx customapi list --publisher nn --type Function", "Powershell");

			writer.WriteParagraph("Show full signatures (useful for exploring available APIs and their arguments):");
			writer.WriteCodeBlock("pacx customapi list --full", "Powershell");
			writer.WriteCodeBlock("pacx customapi list --publisher nn --full", "Powershell");
		}
	}
}
