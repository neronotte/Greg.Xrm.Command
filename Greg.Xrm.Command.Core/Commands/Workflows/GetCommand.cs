using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "get", HelpText = "Returns the definition of a workflow (Power Automate Flow)")]
	[Alias("flow", "get")]
	public class GetCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name of the workflow to retrieve. Provide either the name or the id.")]
		public string Name { get; set; } = string.Empty;

		[Option("id", "i", Order = 2, HelpText = "The id of the workflow to retrieve, as found in the url of the flow designer. Provide either the name or the id.")]
		public Guid? Id { get; set; }

		[Option("solution", "s", Order = 3, HelpText = "The solution that contains the workflow. Only needed when more than one workflow has the same name.")]
		public string? SolutionName { get; set; }

		[Option("output", "o", Order = 4, HelpText = "If specified, the definition is written to this file instead of the console. The folder must exist.")]
		public string? OutputFile { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Name) && !Id.HasValue)
			{
				yield return new ValidationResult("Please provide either the --name or the --id of the workflow to retrieve.", [nameof(Name), nameof(Id)]);
			}

			if (!string.IsNullOrWhiteSpace(Name) && Id.HasValue)
			{
				yield return new ValidationResult("The --name and the --id arguments cannot be used together.", [nameof(Name), nameof(Id)]);
			}
		}


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command returns the definition of a single workflow. For a Power Automate Flow that is the json definition with its trigger, actions and connection references, for a classic workflow it is the xaml definition.");

			writer.WriteParagraph("Use ")
				.WriteCode("pacx workflow list")
				.Write(" first if you do not know the exact name.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --name \"My Flow\"");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("Definitions tend to be large, so you can write them to a file instead of the console. This is handy to compare the same flow between two environments, or to keep it next to the rest of your source code.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --name \"My Flow\" --output C:\\temp\\myflow.json");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("If more than one workflow has the same name, you can tell them apart by the solution that contains them.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --name \"My Flow\" --solution \"My Solution Name\"");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("You can also use the id instead of the name. It is the second guid in the url when you open a flow in the designer, and it is also shown by ")
				.WriteCode("pacx workflow list")
				.Write(".");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --id 507db5fe-17f1-f011-8406-6045bd95f82d");
			writer.WriteCodeBlockEnd();
		}
	}
}
