using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "remove-param",
		HelpText = "Removes a request parameter from an existing Dataverse Custom API.")]
	public class RemoveCustomApiParamCommand : ICanProvideUsageExample
	{
		[Option("api", "a", Order = 1,
			HelpText = "Unique name of the target Custom API (e.g. nn_GregSum).")]
		[Required]
		public string? ApiUniqueName { get; set; }

		[Option("name", "n", Order = 2,
			HelpText = "Short name of the request parameter to remove (without publisher prefix and without -in- segment), e.g. Addend1.")]
		[Required]
		public string? ParamUniqueName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Remove the Addend1 request parameter from a Custom API:");
			writer.WriteCodeBlock("pacx customapi remove-param -a nn_GregSum -n Addend1", "Powershell");

			writer.WriteParagraph("The full Dataverse unique name of the parameter ({api}-in-{name}) is computed automatically. You only need to supply the short name.");
		}
	}
}
