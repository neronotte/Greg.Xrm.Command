using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "run",
		HelpText = "Executes a Dataverse Custom API, passing input parameters from a JSON string or file and printing the response.")]
	public class RunCustomApiCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("unique-name", "n", Order = 1,
			HelpText = "Unique name of the Custom API to execute (e.g. nn_GregSum).")]
		[Required]
		public string? UniqueName { get; set; }

		[Option("input", "i", Order = 2,
			HelpText = "Input parameters as an inline JSON object, e.g. {\"Addend1\":5,\"Addend2\":3}. Mutually exclusive with --input-file.")]
		public string? Input { get; set; }

		[Option("input-file", "f", Order = 3,
			HelpText = "Path to a JSON file containing input parameters. Mutually exclusive with --input.")]
		public string? InputFile { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Input != null && InputFile != null)
				yield return new ValidationResult(
					"--input and --input-file are mutually exclusive.",
					[nameof(Input), nameof(InputFile)]);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Execute a Custom API passing parameters as an inline JSON string:");
			writer.WriteCodeBlock("pacx customapi run -n nn_GregSum -i \"{\\\"Addend1\\\":5,\\\"Addend2\\\":3}\"", "Powershell");

			writer.WriteParagraph("Execute using a JSON file for the input:");
			writer.WriteCodeBlock("pacx customapi run -n nn_GregSum --input-file .\\params.json", "Powershell");

			writer.WriteParagraph("Execute a Custom API that requires no input parameters:");
			writer.WriteCodeBlock("pacx customapi run -n nn_Ping", "Powershell");

			writer.WriteParagraph("JSON keys are the short parameter names (without publisher prefix and without the -in- segment). Supported scalar types: Boolean, DateTime, Decimal, Float, Guid, Integer, Money, Picklist, String, StringArray. For EntityReference use {\"logicalname\":\"account\",\"id\":\"<guid>\"}.");
		}
	}
}
