using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "create",
		HelpText = "Creates a Dataverse Custom API with optional request parameters and response properties.")]
	public class CreateCustomApiCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("unique-name", "n", Order = 1,
			HelpText = "Unique name of the Custom API, including publisher prefix (e.g. nn_GregSum).")]
		[Required]
		public string? UniqueName { get; set; }

		[Option("display-name", "d", Order = 2,
			HelpText = "Display name. Defaults to the unique name without prefix, split on camel-case.")]
		public string? DisplayName { get; set; }

		[Option("description", "desc", Order = 3, DefaultValue = "",
			HelpText = "Description of the Custom API.")]
		public string Description { get; set; } = string.Empty;

		[Option("binding-type", "b", Order = 4, DefaultValue = CustomApiBindingType.Global,
			HelpText = "Binding type: Global, Entity, or EntityCollection.")]
		public CustomApiBindingType BindingType { get; set; } = CustomApiBindingType.Global;

		[Option("type", "t", Order = 5, DefaultValue = CustomApiType.Action,
			HelpText = "Action (POST) or Function (GET).")]
		public CustomApiType Type { get; set; } = CustomApiType.Action;

		[Option("is-private", Order = 6, DefaultValue = false,
			HelpText = "Whether the Custom API is private.")]
		public bool IsPrivate { get; set; } = false;

		[Option("allowed-step-type", "ast", Order = 7, DefaultValue = CustomApiAllowedStepType.SyncAndAsync,
			HelpText = "Allowed processing step type: None, AsyncOnly, or SyncAndAsync.")]
		public CustomApiAllowedStepType AllowedStepType { get; set; } = CustomApiAllowedStepType.SyncAndAsync;

		[Option("execute-privilege", "ep", Order = 8, DefaultValue = "",
			HelpText = "Name of the privilege required to execute this Custom API.")]
		public string ExecutePrivilegeName { get; set; } = string.Empty;

		[Option("param", "p", Order = 9,
			HelpText = "Comma-separated request parameters as Name:Type (required) or Name?:Type (optional). E.g. nn_X:Integer,nn_Y?:String")]
		public string? Params { get; set; }

		[Option("response", "r", Order = 10,
			HelpText = "Comma-separated response properties as Name:Type. E.g. nn_Result:Integer")]
		public string? Responses { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext context)
		{
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
			writer.WriteParagraph("Create a minimal Custom API (API record only):");
			writer.WriteCodeBlock("pacx customapi create -n nn_GregSum", "Powershell");
			writer.WriteParagraph("Create with request parameters and a response property (comma-separated):");
			writer.WriteCodeBlock("pacx customapi create -n nn_GregSum -p \"Addend1:Integer, Addend2:Integer\" -r Result:Integer", "Powershell");
		}
	}
}
