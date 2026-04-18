using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "generateLateBoundConstants", HelpText = "Generates C# and/or JavaScript constants files from Dataverse metadata for a solution.")]
	[Alias("solution", "constants")]
	[Alias("solution", "late-bound")]
	[Alias("solution", "lateBound")]
	public class GenerateLateBoundConstantsCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("solutionName", "sn", Order = 1, HelpText = "The unique name of the solution to extract constants for. Uses the current default solution if omitted.")]
		public string? Solution { get; set; }

		[Option("outputCs", "ocs", Order = 10, HelpText = "Output folder path for generated C# constants files. If omitted, no C# files are generated.")]
		public string? OutputCs { get; set; }

		[Option("namespaceCs", "ncs", Order = 11, HelpText = "C# namespace for the generated constants classes. Required when --outputCs is specified.")]
		public string? NamespaceCs { get; set; }

		[Option("outputJs", "ojs", Order = 20, HelpText = "Output folder path for generated JavaScript constants files. If omitted, no JS files are generated.")]
		public string? OutputJs { get; set; }

		[Option("namespaceJs", "njs", Order = 21, HelpText = "JavaScript root namespace object for the generated constants. Required when --outputJs is specified.")]
		public string? NamespaceJs { get; set; }

		[Option("jsHeader", "jsh", Order = 22, HelpText = "Header lines to prepend to each generated JavaScript file. Use \\n to separate multiple lines.")]
		public string? JsHeader { get; set; }

		[Option("withTypes", "wt", Order = 30, HelpText = "Include attribute type information in C# XML doc comments.", DefaultValue = true)]
		public bool WithTypes { get; set; } = true;

		[Option("withDescriptions", "wd", Order = 31, HelpText = "Include attribute description in C# XML doc comments.", DefaultValue = true)]
		public bool WithDescriptions { get; set; } = true;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(OutputCs) && string.IsNullOrWhiteSpace(OutputJs))
				yield return new ValidationResult(
					"At least one of --outputCs or --outputJs must be provided.",
					new[] { nameof(OutputCs), nameof(OutputJs) });

			if (!string.IsNullOrWhiteSpace(OutputCs) && string.IsNullOrWhiteSpace(NamespaceCs))
				yield return new ValidationResult(
					"--namespaceCs is required when --outputCs is specified.",
					new[] { nameof(NamespaceCs), nameof(OutputCs) });

			if (!string.IsNullOrWhiteSpace(OutputJs) && string.IsNullOrWhiteSpace(NamespaceJs))
				yield return new ValidationResult(
					"--namespaceJs is required when --outputJs is specified.",
					new[] { nameof(NamespaceJs), nameof(OutputJs) });
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Generate C# constants for a solution:");
			writer.WriteCodeBlock(
				"pacx solution constants --solutionName MySolution --outputCs \"C:/src/Constants\" --namespaceCs \"MyApp.Constants\"",
				"PowerShell");

			writer.WriteParagraph("Generate both C# and JavaScript constants:");
			writer.WriteCodeBlock(
				"pacx solution constants --solutionName MySolution --outputCs \"C:/src/Constants\" --namespaceCs \"MyApp.Constants\" --outputJs \"C:/src/js/constants\" --namespaceJs \"MyApp\"",
				"PowerShell");

			writer.WriteParagraph("Generate JS only, using the default solution:");
			writer.WriteCodeBlock(
				"pacx solution constants --outputJs \"C:/src/js/constants\" --namespaceJs \"MyApp\"",
				"PowerShell");
		}
	}
}
