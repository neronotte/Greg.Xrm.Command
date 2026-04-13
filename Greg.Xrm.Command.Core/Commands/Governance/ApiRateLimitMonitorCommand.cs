using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Governance
{
	[Command("api", "ratelimit", "monitor", HelpText = "Monitor API rate limit usage and alert when approaching thresholds.")]
	public class ApiRateLimitMonitorCommand
	{
		[Option("period", "p", Order = 1, DefaultValue = "hour", HelpText = "Time period: minute, hour, day.")]
		public string Period { get; set; } = "hour";

		[Option("threshold", "t", Order = 2, DefaultValue = 80, HelpText = "Alert threshold as percentage of limit (default: 80%).")]
		public int Threshold { get; set; } = 80;

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("alert", Order = 4, HelpText = "Send alert when threshold is exceeded.")]
		public bool Alert { get; set; }
	}
}
