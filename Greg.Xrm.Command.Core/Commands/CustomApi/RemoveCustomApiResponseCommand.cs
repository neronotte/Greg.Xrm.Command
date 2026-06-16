using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "remove-response",
		HelpText = "Removes a response property from an existing Dataverse Custom API.")]
	public class RemoveCustomApiResponseCommand : ICanProvideUsageExample
	{
		[Option("api", "a", Order = 1,
			HelpText = "Unique name of the target Custom API (e.g. nn_GregSum).")]
		[Required]
		public string? ApiUniqueName { get; set; }

		[Option("name", "n", Order = 2,
			HelpText = "Short name of the response property to remove (without publisher prefix and without -out- segment), e.g. Result.")]
		[Required]
		public string? ResponseUniqueName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Remove the Result response property from a Custom API:");
			writer.WriteCodeBlock("pacx customapi remove-response -a nn_GregSum -n Result", "Powershell");

			writer.WriteParagraph("The full Dataverse unique name of the property ({api}-out-{name}) is computed automatically. You only need to supply the short name.");
		}
	}
}
