using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "create", HelpText = "Creates a new Power Automate Flow from a json definition file")]
	[Alias("flow", "create")]
	public class CreateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The name of the flow to create.")]
		[Required]
		public string Name { get; set; } = string.Empty;

		[Option("file", "f", Order = 2, HelpText = "The json file containing the definition of the flow (the same format returned by 'pacx workflow get').")]
		[Required]
		public string DefinitionFile { get; set; } = string.Empty;

		[Option("solution", "s", Order = 3, HelpText = "The solution that will contain the flow. If not specified, the current default solution is used.")]
		public string? SolutionName { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				yield return new ValidationResult("Please provide the --name of the flow to create.", [nameof(Name)]);
			}

			if (string.IsNullOrWhiteSpace(DefinitionFile))
			{
				yield return new ValidationResult("Please provide the --file containing the definition of the flow.", [nameof(DefinitionFile)]);
			}
		}


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command creates a modern flow from a json definition file. The definition format is the one returned by ")
				.WriteCode("pacx workflow get")
				.Write(", so the easiest way to author one is to export a similar flow and change what you need.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --name \"My Flow\" --output myflow.json");
			writer.WriteLine("# edit myflow.json, then:");
			writer.WriteLine("pacx workflow create --name \"My New Flow\" --file myflow.json");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("Before the flow is created, the definition is validated. The connection references it uses must exist in the environment, and the definition itself is checked by the flow engine through a short-lived flow named ")
				.WriteCode("pacx validation probe ...")
				.Write(" that is created, activated once (automatic triggers get an always-false condition, so the probe cannot fire), and removed again. If the command is killed in that moment, such a probe flow can be left behind and can simply be deleted.");

			writer.WriteParagraph("The flow is created in draft state, so nothing runs until you review it. Open it in the flow designer to check that the connection references resolve, then activate it with ")
				.WriteCode("pacx workflow activate")
				.Write(".");

			writer.WriteParagraph("By default the flow is added to the current default solution. Use the --solution option to pick a different one.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow create --name \"My New Flow\" --file myflow.json --solution mysolution");
			writer.WriteCodeBlockEnd();
		}
	}
}
