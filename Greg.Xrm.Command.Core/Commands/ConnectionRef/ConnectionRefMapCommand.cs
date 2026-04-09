using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.ConnectionRef
{
	[Command("connection-ref", "map", HelpText = "Map connection references across solutions and environments.")]
	public class ConnectionRefMapCommand
	{
		[Option("solution", "s", Order = 1, HelpText = "Filter by solution unique name.")]
		public string? SolutionUniqueName { get; set; }

		[Option("connector", "c", Order = 2, HelpText = "Filter by connector ID or name.")]
		public string? ConnectorId { get; set; }

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("interactive", "i", Order = 4, HelpText = "Interactive mode to update connection references.")]
		public bool Interactive { get; set; }
	}
}
