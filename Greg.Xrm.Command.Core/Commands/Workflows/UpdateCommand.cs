using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[Command("workflow", "update", HelpText = "Replaces the definition of an existing Power Automate Flow with the content of a json definition file")]
	[Alias("flow", "update")]
	public class UpdateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name of the workflow to update. Provide either the name or the id.")]
		public string Name { get; set; } = string.Empty;

		[Option("id", "i", Order = 2, HelpText = "The id of the workflow to update, as found in the url of the flow designer. Provide either the name or the id.")]
		public Guid? Id { get; set; }

		[Option("file", "f", Order = 3, HelpText = "The json file containing the new definition of the flow (the same format returned by 'pacx workflow get').")]
		public string DefinitionFile { get; set; } = string.Empty;


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(Name) && !Id.HasValue)
			{
				yield return new ValidationResult("Please provide either the --name or the --id of the workflow to update.", [nameof(Name), nameof(Id)]);
			}

			if (!string.IsNullOrWhiteSpace(Name) && Id.HasValue)
			{
				yield return new ValidationResult("The --name and the --id arguments cannot be used together.", [nameof(Name), nameof(Id)]);
			}

			if (string.IsNullOrWhiteSpace(DefinitionFile))
			{
				yield return new ValidationResult("Please provide the --file containing the new definition of the flow.", [nameof(DefinitionFile)]);
			}
		}


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command replaces the definition of an existing modern flow with the content of a json file. Together with ")
				.WriteCode("pacx workflow get")
				.Write(" and ")
				.WriteCode("pacx workflow create")
				.Write(" it allows a full edit cycle from the command line.");

			writer.WriteParagraph("Export the current definition first: it is your backup, and the natural starting point for the changes.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow get --name \"My Flow\" --output myflow.json");
			writer.WriteLine("# edit myflow.json, then:");
			writer.WriteLine("pacx workflow update --name \"My Flow\" --file myflow.json");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("If the flow is currently activated, the new definition becomes effective immediately, the same way it does when you save from the flow designer.");

			writer.WriteParagraph("You can also use the id instead of the name. It is the second guid in the url when you open the flow in the designer.");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx workflow update --id 507db5fe-17f1-f011-8406-6045bd95f82d --file myflow.json");
			writer.WriteCodeBlockEnd();
		}
	}
}
