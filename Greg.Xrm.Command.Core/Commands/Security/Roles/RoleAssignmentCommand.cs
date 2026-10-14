using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public abstract class RoleAssignmentCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("role", "r", Order = 1, HelpText = "Exact security role name or GUID of the role in the selected business unit.")]
		[Required]
		public string Role { get; set; } = string.Empty;

		[Option("user", "u", Order = 2, HelpText = "User GUID, domain name or primary email. Specify --user, --team, or both.")]
		public string? User { get; set; }

		[Option("team", "t", Order = 3, HelpText = "Team GUID or exact name. Specify --user, --team, or both.")]
		public string? Team { get; set; }

		[Option("businessunit", "bu", Order = 4, HelpText = "Business unit GUID or exact name. Required when record ownership across business units is enabled; ignored otherwise.")]
		public string? BusinessUnit { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(this.User) && string.IsNullOrWhiteSpace(this.Team))
				yield return new ValidationResult("Specify at least one of --user or --team.", [nameof(User), nameof(Team)]);
		}

		public abstract void WriteUsageExamples(MarkdownWriter writer);
	}
}