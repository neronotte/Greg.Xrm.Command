using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Mcp
{
	[Command("mcp", "start", HelpText = "Start the MCP server to expose PACX commands to AI agents.")]
	public class McpStartCommand
	{
		[Option("port", "p", Order = 1, DefaultValue = 3000, HelpText = "Port to listen on. Default is 3000.")]
		public int Port { get; set; } = 3000;

		[Option("transport", "t", Order = 2, DefaultValue = "stdio", HelpText = "Transport type: stdio, http. Default is stdio.")]
		public string Transport { get; set; } = "stdio";

		[Option("host", Order = 3, DefaultValue = "localhost", HelpText = "Host to bind to (for HTTP transport).")]
		public string Host { get; set; } = "localhost";
	}
}
