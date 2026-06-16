using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "describe",
		HelpText = "Shows the full signature and metadata of a Dataverse Custom API, including its request parameters and response properties.")]
	public class DescribeCustomApiCommand : ICanProvideUsageExample
	{
		[Option("unique-name", "n", Order = 1,
			HelpText = "Unique name of the Custom API to describe (e.g. nn_GregSum).")]
		[Required]
		public string? UniqueName { get; set; }

		[Option("generate-input-file", "gif", Order = 10,
			HelpText = "When specified, writes a sample JSON input file at this path, ready to pass to 'customapi run --input-file'.")]
		public string? GenerateInputFile { get; set; }

		[Option("generate-schema-file", "gsf", Order = 11,
			HelpText = "When specified, writes a JSON Schema file at this path describing the expected input parameters.")]
		public string? GenerateSchemaFile { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Show the full signature and metadata of a Custom API:");
			writer.WriteCodeBlock("pacx customapi describe -n nn_GregSum", "Powershell");

			writer.WriteParagraph("Generate a sample input file to use with 'customapi run':");
			writer.WriteCodeBlock("pacx customapi describe -n nn_GregSum --generate-input-file input.json", "Powershell");

			writer.WriteParagraph("Generate a JSON Schema for the input parameters (useful for validation or IDE autocompletion):");
			writer.WriteCodeBlock("pacx customapi describe -n nn_GregSum --generate-schema-file schema.json", "Powershell");

			writer.WriteParagraph("Both flags can be combined in a single call:");
			writer.WriteCodeBlock("pacx customapi describe -n nn_GregSum -gif input.json -gsf schema.json", "Powershell");

			writer.WriteParagraph("The output includes the API type (Action/Function), binding, privacy, allowed step types, bound plugin, description, all request parameters (name, type, required flag, description), and all response properties (name, type, description).");
		}
	}
}
