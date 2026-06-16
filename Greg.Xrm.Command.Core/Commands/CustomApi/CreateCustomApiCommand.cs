using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "create",
		HelpText = "Creates a Dataverse Custom API with optional request parameters and response properties.")]
	public class CreateCustomApiCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("display-name", "d", Order = 1,
			HelpText = "Human-readable name of the Custom API (e.g. 'Greg Sum').")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("unique-name", "n", Order = 2,
			HelpText = "Unique name including publisher prefix (e.g. nn_GregSum). Inferred from --display-name and the solution publisher prefix if omitted.")]
		public string? UniqueName { get; set; }

		[Option("description", "desc", Order = 3, DefaultValue = "",
			HelpText = "Description of the Custom API.")]
		public string Description { get; set; } = string.Empty;

		[Option("binding-type", "b", Order = 4, DefaultValue = CustomApiBindingType.Global,
			HelpText = "Binding type: Global, Entity, or EntityCollection.")]
		public CustomApiBindingType BindingType { get; set; } = CustomApiBindingType.Global;

		[Option("type", "t", Order = 5, DefaultValue = CustomApiType.Action,
			HelpText = "Action (POST) or Function (GET).")]
		public CustomApiType Type { get; set; } = CustomApiType.Action;

		[Option("allowed-step-type", "ast", Order = 6, DefaultValue = CustomApiAllowedStepType.SyncAndAsync,
			HelpText = "Allowed processing step type: None, AsyncOnly, or SyncAndAsync.")]
		public CustomApiAllowedStepType AllowedStepType { get; set; } = CustomApiAllowedStepType.SyncAndAsync;

		[Option("execute-privilege", "ep", Order = 8, DefaultValue = "",
			HelpText = "Name of the privilege required to execute this Custom API.")]
		public string ExecutePrivilegeName { get; set; } = string.Empty;

		[Option("param", "p", Order = 9,
			HelpText = "Comma-separated request parameters as Name:Type (required) or Name?:Type (optional). E.g. X:Integer,Y?:String")]
		public string? Params { get; set; }

		[Option("response", "r", Order = 10,
			HelpText = "Comma-separated response properties as Name:Type. E.g. Result:Integer")]
		public string? Responses { get; set; }

		[Option("solution", "s", Order = 50,
			HelpText = "Unmanaged solution unique name to add the Custom API components to. Uses the current default solution if omitted.")]
		public string? SolutionName { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext context)
		{
			if (!string.IsNullOrWhiteSpace(UniqueName) && !UniqueName.Contains('_'))
				yield return new ValidationResult(
					"--unique-name must include a publisher prefix separated by '_' (e.g. nn_GregSum).",
					new[] { nameof(UniqueName) });

			foreach (var p in SplitSpecs(Params))
				if (!CustomApiParamSpec.TryParse(p, out _, out var err))
					yield return new ValidationResult($"Invalid --param '{p}': {err}");

			foreach (var r in SplitSpecs(Responses))
				if (!CustomApiParamSpec.TryParse(r, out _, out var err))
					yield return new ValidationResult($"Invalid --response '{r}': {err}");
		}

		internal static IEnumerable<string> SplitSpecs(string? value)
			=> string.IsNullOrWhiteSpace(value)
				? []
				: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Create a minimal Custom API (unique name inferred from display name + solution publisher prefix):");
			writer.WriteCodeBlock("pacx customapi create -d \"Greg Sum\" -s MySolution", "Powershell");

			writer.WriteParagraph("Create with an explicit unique name:");
			writer.WriteCodeBlock("pacx customapi create -d \"Greg Sum\" -n nn_GregSum -s MySolution", "Powershell");

			writer.WriteParagraph("Create with request parameters and a response property:");
			writer.WriteCodeBlock("pacx customapi create -d \"Greg Sum\" -p \"Addend1:Integer, Addend2:Integer\" -r Result:Integer -s MySolution", "Powershell");

			writer.WriteParagraph("Naming conventions applied automatically:");
			writer.WriteParagraph(
				"- **Unique name** (when omitted): `{publisherPrefix}_{DisplayNameWithoutSpaces}` — e.g. display name 'Greg Sum' with prefix 'nn' becomes `nn_GregSum`.\n" +
				"- **Request parameter unique names**: `{customApiUniqueName}-in-{parameterName}` — e.g. `nn_GregSum-in-Addend1`.\n" +
				"- **Response property unique names**: `{customApiUniqueName}-out-{propertyName}` — e.g. `nn_GregSum-out-Result`.\n" +
				"- **Display names** for parameters/responses: inferred from the name by splitting on capital-letter boundaries — e.g. `Addend1` becomes 'Addend 1'.\n" +
				"- **Publisher prefix validation**: if `--unique-name` is provided, its prefix (before `_`) must match the solution publisher prefix; mismatches are rejected."
			);
		}
	}
}
