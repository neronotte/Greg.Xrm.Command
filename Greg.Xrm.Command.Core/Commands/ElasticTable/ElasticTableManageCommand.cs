using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.ElasticTable
{
	[Command("elastic-table", "manage", HelpText = "Manage Elastic Table retention policies and scaling.")]
	public class ElasticTableManageCommand
	{
		[Option("table", "t", Order = 1, Required = true, HelpText = "Elastic table logical name.")]
		public string TableLogicalName { get; set; } = "";

		[Option("retention", "r", Order = 2, HelpText = "Retention period (e.g., '90d', '6m', '1y').")]
		public string? RetentionPeriod { get; set; }

		[Option("scale", "s", Order = 3, HelpText = "Scale capacity setting.")]
		public string? ScaleCapacity { get; set; }

		[Option("changelog", Order = 4, HelpText = "Enable/disable change feed tracking.")]
		public bool? EnableChangefeed { get; set; }

		[Option("show", Order = 5, HelpText = "Show current elastic table configuration.")]
		public bool ShowConfig { get; set; }

		[Option("format", "f", Order = 6, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
