using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[Command("unifiedrouting", "queueStatus", HelpText = "List the agents in the queue provided. Optionally, you can specify a date in order to list agents status at that time. It uses the Dataverse environment selected using `pacx auth select`")]
	[Alias("ur","queueStatus")]
	public class GetQueueStatusCommand : ICanProvideUsageExample
	{
		[Option("queue", "q", "Queue name used to perform the query.")]
		[Required]
		public string? Queue { get; set; }

		[Option("dateTime", "t", "Date and time (local time) used to perform the query. Format dd/MM/yyyy HH:mm.")]
		public string? DateTimeFilter { get; set; }

        [Option("isUnitBased", "ub", DefaultValue = false)]
        public bool UnitBased { get; set; } = false;

        public void WriteUsageExamples(MarkdownWriter writer)
		{

		}
	}
}
