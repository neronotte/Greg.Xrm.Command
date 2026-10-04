using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[Command("security", "users", "list", HelpText = "List the users defined in the environment, optionally filtered by id or by (partial) first name, last name or domain name.")]
	[Alias("security", "user", "list")]
	public class ListCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("name", "n", Order = 1, HelpText = "Optional filter. If it's a GUID, returns the user with that id (other filters are ignored); otherwise returns the users whose first name, last name or domain name contains the given text.")]
		public string? Name { get; set; }

		[Option("include-disabled", "d", Order = 10, DefaultValue = false, HelpText = "If specified, disabled users (including SYSTEM and INTEGRATION) are returned too.")]
		public bool IncludeDisabled { get; set; }

		[Option("include-app-users", "app", Order = 11, DefaultValue = false, HelpText = "If specified, application users (service principals) are returned too.")]
		public bool IncludeApplicationUsers { get; set; }

		[Option("top", "t", Order = 20, HelpText = "If specified, limits the number of returned users.")]
		public int? Top { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Top.HasValue && Top.Value <= 0)
			{
				yield return new ValidationResult("The value of --top must be greater than zero.", [nameof(Top)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("List all the enabled users defined in the environment (disabled users and application users are excluded by default):");
			writer.WriteCodeBlock("pacx security user list", "Powershell");

			writer.WriteParagraph("List the users whose first name, last name or domain name contains a given text (case insensitive):");
			writer.WriteCodeBlock("pacx security user list -n mario", "Powershell");

			writer.WriteParagraph("Retrieve a user by id (the user is returned even if disabled or if it's an application user):");
			writer.WriteCodeBlock("pacx security user list --name 3f2504e0-4f89-11d3-9a0c-0305e82c3301", "Powershell");

			writer.WriteParagraph("Include disabled users and application users, returning at most 50 rows:");
			writer.WriteCodeBlock("pacx security user list --include-disabled --include-app-users --top 50", "Powershell");
			writer.WriteCodeBlock("pacx security user list -d -app -t 50", "Powershell");
		}
	}
}
