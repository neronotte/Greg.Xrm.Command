using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "add-response",
		HelpText = "Adds a response property to an existing Dataverse Custom API.")]
	public class AddCustomApiResponseCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("api", "a", Order = 1,
			HelpText = "Unique name of the target Custom API (e.g. nn_GregSum).")]
		[Required]
		public string? ApiUniqueName { get; set; }

		[Option("response", "r", Order = 2,
			HelpText = "Response property spec as Name:Type. Name must not include publisher prefix — the unique name is built as {api}-out-{name}.")]
		[Required]
		public string? Response { get; set; }

		[Option("display-name", "d", Order = 3,
			HelpText = "Display name. Defaults to the property name without prefix, split on camel-case.")]
		public string? DisplayName { get; set; }

		[Option("description", "desc", Order = 4, DefaultValue = "",
			HelpText = "Description of the response property.")]
		public string Description { get; set; } = string.Empty;

		public IEnumerable<ValidationResult> Validate(ValidationContext context)
		{
			if (Response is not null && !CustomApiParamSpec.TryParse(Response, out _, out var err))
				yield return new ValidationResult($"Invalid --response '{Response}': {err}");
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Add an Integer response property to an existing Custom API:");
			writer.WriteCodeBlock("pacx customapi add-response -a nn_GregSum -r Result:Integer", "Powershell");

			writer.WriteParagraph("Add an EntityReference response with a description:");
			writer.WriteCodeBlock("pacx customapi add-response -a nn_GregCase -r ResolvedCase:EntityReference --description \"The case resolved by this action\"", "Powershell");

			writer.WriteParagraph("Supported types: Boolean, DateTime, Decimal, Entity, EntityCollection, EntityReference, Float, Integer, Money, Picklist, String, StringArray, Guid.");
		}
	}
}
