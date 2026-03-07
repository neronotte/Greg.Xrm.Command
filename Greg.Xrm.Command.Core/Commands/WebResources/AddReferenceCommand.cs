using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "addReference", HelpText = "Adds an external web resource to the current webresource project **as a reference**, without copying it locally.")]
	[Alias("webresources", "add-reference")]
	[Alias("wr", "add-reference")]
	[Alias("wr", "add-ref")]
	[Alias("wr", "addRef")]
	public class AddReferenceCommand : ICanProvideUsageExample
	{
		[Option("source", "src", Order = 1, HelpText = "The absolute or relative URL of the web resource to add as reference to the current project.")]
		[Required]
		public string Source { get; set; } = string.Empty;

		[Option("target", "tgt", Order = 2, HelpText = "The target URL of the webresource, relative to the root of the dataverse WebResources. It must include the publisher prefix.")]
		[Required]
		public string Target { get; set; } = string.Empty;

		[Option("path", "p", Order = 3, HelpText = "The folder containing the .wr.pacx project where the reference should be added. If not specified, the command will find it recoursing up from the current folder.")]
		public string? Path { get; set; }

		[Option("solution", "s", Order = 4, HelpText = "The name of the solution that will contain the WebResources. If empty, the default solution for the current environment is used as default")]
		public string? SolutionName { get; set; }




		public void WriteUsageExamples(MarkdownWriter writer)
		{
			
		}
	}
}
