using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "add-param",
		HelpText = "Adds a request parameter to an existing Dataverse Custom API.")]
	public class AddCustomApiParamCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("api", "a", Order = 1,
			HelpText = "Unique name of the target Custom API (e.g. nn_GregSum).")]
		[Required]
		public string? ApiUniqueName { get; set; }

		[Option("param", "p", Order = 2,
			HelpText = "Parameter spec as Name:Type (required) or Name?:Type (optional). Name must not include publisher prefix — the unique name is built as {api}-in-{name}.")]
		[Required]
		public string? Param { get; set; }

		[Option("display-name", "d", Order = 3,
			HelpText = "Display name. Defaults to the parameter name without prefix, split on camel-case.")]
		public string? DisplayName { get; set; }

		[Option("description", "desc", Order = 4, DefaultValue = "",
			HelpText = "Description of the parameter.")]
		public string Description { get; set; } = string.Empty;

		public IEnumerable<ValidationResult> Validate(ValidationContext context)
		{
			if (Param is not null && !CustomApiParamSpec.TryParse(Param, out _, out var err))
				yield return new ValidationResult($"Invalid --param '{Param}': {err}");
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Add a required Integer parameter to an existing Custom API:");
			writer.WriteCodeBlock("pacx customapi add-param -a nn_GregSum -p Addend1:Integer", "Powershell");

			writer.WriteParagraph("Add an optional String parameter with a custom display name:");
			writer.WriteCodeBlock("pacx customapi add-param -a nn_GregSum -p Comment?:String -d \"Optional Comment\"", "Powershell");

			writer.WriteParagraph("Supported types: Boolean, DateTime, Decimal, Entity, EntityCollection, EntityReference, Float, Integer, Money, Picklist, String, StringArray, Guid.");
		}
	}
}
