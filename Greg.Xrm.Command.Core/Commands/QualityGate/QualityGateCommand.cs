using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.QualityGate
{
	[Command("quality", "gate", HelpText = "Parse pac solution check results and fail CI on high severity issues.")]
	public class QualityGateCommand
	{
		[Option("input", "i", Order = 1, HelpText = "Path to the solution check result ZIP or directory. Auto-detects if not provided.")]
		public string? InputPath { get; set; }

		[Option("fail-on", Order = 2, DefaultValue = "High", HelpText = "Minimum severity to fail: Error, High, Medium, Low.")]
		public string FailOnSeverity { get; set; } = "High";

		[Option("format", "f", Order = 3, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("solution", "s", Order = 4, HelpText = "Run solution check on this solution before gating.")]
		public string? SolutionUniqueName { get; set; }
	}
}
