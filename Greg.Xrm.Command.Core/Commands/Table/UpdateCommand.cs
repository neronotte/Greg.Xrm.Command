using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table
{
    public class UpdateCommand : IValidatableObject
	{
		[Option("schemaName", "sn", HelpText = "The schema name of the table to delete")]
		[Required]
		public string? SchemaName { get; set; }

		[Option("name", "n", HelpText = "The display name of the table to be created.")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("plural", "p", HelpText = "The collection name of the table to be created.")]
		public string? DisplayCollectionName { get; set; }


		[Option("feedback", "fb", DefaultValue = false, HelpText = "Indicates whether user can provide feedbacks to records in this table or not. It can only be set.")]
		public bool? HasFeedback { get; set; }

		[Option("notes", DefaultValue = false, HelpText = "Indicates whether user can add notes and attachments to the current table or not. It can only be set.")]
		public bool? HasNotes { get; set; }

		[Option("audit", "a", DefaultValue = true, HelpText = "Indicates whether audit is enabled or not.")]
		public bool? IsAuditEnabled { get; set; }

		[Option("changeTracking", "ct", DefaultValue = null, HelpText = "Indicates whether change tracking is enabled or not.")]
		public bool? ChangeTrackingEnabled { get; set; }

		[Option("quickCreate", "qc", DefaultValue = false, HelpText = "Indicates whether quick create form is enabled or not.")]
		public bool? IsQuickCreateEnabled { get; set; }

		[Option("hasEmail", "email", DefaultValue = false, HelpText = "Rows in this table can have email addresses (for example, info@contoso.com.). If the table didn’t have an email column, one will be added. This option can only be set.")]
		public bool? HasEmailAddresses { get; set; }




        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (HasFeedback.HasValue && !HasFeedback.Value)
			{
				yield return new ValidationResult("The option --feedback can only be set to true.", [nameof(HasEmailAddresses)]);
			}

			if (HasNotes.HasValue && !HasNotes.Value)
			{
				yield return new ValidationResult("The option --notes can only be set to true.", [nameof(HasEmailAddresses)]);
			}

			if (HasEmailAddresses.HasValue && !HasEmailAddresses.Value)
			{
				yield return new ValidationResult("The option --hasEmail can only be set to true.", [nameof(HasEmailAddresses)]);
			}

		}
    }
}
