using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "delete",
		HelpText = "Deletes a Dataverse Custom API and all its request parameters and response properties.")]
	public class DeleteCustomApiCommand : ICanProvideUsageExample
	{
		[Option("unique-name", "n", Order = 1,
			HelpText = "Unique name of the Custom API to delete (e.g. nn_GregSum).")]
		[Required]
		public string? UniqueName { get; set; }

		[Option("force", Order = 2, DefaultValue = false,
				HelpText = "Skip the interactive confirmation prompt and delete immediately.")]
			public bool Force { get; set; } = false;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Delete a Custom API with an interactive confirmation prompt:");
			writer.WriteCodeBlock("pacx customapi delete -n nn_GregSum", "Powershell");

			writer.WriteParagraph("Delete without confirmation (useful in scripts and CI pipelines):");
			writer.WriteCodeBlock("pacx customapi delete -n nn_GregSum --force", "Powershell");

			writer.WriteParagraph("This command also removes all associated request parameters and response properties. Use `pacx customapi list` first to verify the target.");
		}
	}
}
