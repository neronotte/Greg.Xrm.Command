using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "update", HelpText = "Updates the metadata of an existing table.")]
	public class UpdateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("schemaName", "sn", Order = 1, HelpText = "The logical name of the table to update.")]
		[Required]
		public string? SchemaName { get; set; }

		[Option("name", "n", Order = 2, HelpText = "The display name of the table.")]
		public string? DisplayName { get; set; }

		[Option("plural", "p", Order = 3, HelpText = "The plural display name of the table.")]
		public string? DisplayCollectionName { get; set; }


		[Option("feedback", "fb", Order = 4, DefaultValue = false, HelpText = "Indicates whether user can provide feedbacks to records in this table or not. It can only be set.")]
		public bool? HasFeedback { get; set; }

		[Option("notes", "notes", Order = 5, DefaultValue = false, HelpText = "Indicates whether user can add notes and attachments to the current table or not. It can only be set.")]
		public bool? HasNotes { get; set; }

		[Option("audit", "a", Order = 6, DefaultValue = true, HelpText = "Indicates whether audit is enabled or not.")]
		public bool? IsAuditEnabled { get; set; }

		[Option("changeTracking", "ct", Order = 7, DefaultValue = null, HelpText = "Indicates whether change tracking is enabled or not.")]
		public bool? ChangeTrackingEnabled { get; set; }

		[Option("quickCreate", "qc", Order = 8, DefaultValue = false, HelpText = "Indicates whether quick create form is enabled or not.")]
		public bool? IsQuickCreateEnabled { get; set; }

		[Option("hasEmail", "email", Order = 9, DefaultValue = false, HelpText = "Rows in this table can have email addresses (for example, info@contoso.com.). If the table didn’t have an email column, one will be added. This option can only be set.")]
		public bool? HasEmailAddresses { get; set; }

		[Option("color", "color", Order = 10, HelpText = "Table color as a hexadecimal code, for example #336699.")]
		public string? Color { get; set; }

		[Option("primaryImage", "image", Order = 11, HelpText = "Logical name of an existing image column to use as the primary image.")]
		public string? PrimaryImage { get; set; }

		[Option("duplicateDetection", "dd", Order = 12, HelpText = "Enable or disable duplicate detection for the table.")]
		public bool? DuplicateDetection { get; set; }

		[Option("customHelp", "help", Order = 13, HelpText = "Enable or disable custom help for the table.")]
		public bool? CustomHelp { get; set; }

		[Option("helpUrl", "url", Order = 14, HelpText = "URL for custom help.")]
		public string? HelpUrl { get; set; }

		[Option("retention", "ret", Order = 15, HelpText = "Enable or disable long-term retention, where supported.")]
		public bool? Retention { get; set; }

		[Option("activities", "activities", Order = 16, HelpText = "Enable activities for the table. This cannot be disabled once enabled.")]
		public bool? Activities { get; set; }

		[Option("mailMerge", "mm", Order = 17, HelpText = "Enable or disable mail merge.")]
		public bool? MailMerge { get; set; }

		[Option("documentManagement", "dm", Order = 18, HelpText = "Enable or disable SharePoint document management for the table.")]
		public bool? DocumentManagement { get; set; }

		[Option("connections", "conn", Order = 19, HelpText = "Enable connections. This cannot be disabled once enabled.")]
		public bool? Connections { get; set; }

		[Option("accessTeams", "at", Order = 20, HelpText = "Enable or disable access teams.")]
		public bool? AccessTeams { get; set; }

		[Option("offline", "off", Order = 21, HelpText = "Enable or disable availability offline.")]
		public bool? Offline { get; set; }

		[Option("queue", "queue", Order = 22, HelpText = "Enable queues for the table. This cannot be disabled once enabled.")]
		public bool? Queue { get; set; }

		[Option("autoRouteToOwnerQueue", "route", Order = 23, HelpText = "Automatically route new or assigned rows to the owner's default queue.")]
		public bool? AutoRouteToOwnerQueue { get; set; }

		[Option("solution", "s", Order = 50, HelpText = "Unmanaged solution unique name. If omitted, the current update behavior is used.")]
		public string? SolutionName { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (HasFeedback.HasValue && !HasFeedback.Value)
			{
				yield return new ValidationResult("The option --feedback can only be set to true.", [nameof(HasFeedback)]);
			}

			if (HasNotes.HasValue && !HasNotes.Value)
			{
				yield return new ValidationResult("The option --notes can only be set to true.", [nameof(HasNotes)]);
			}

			if (HasEmailAddresses.HasValue && !HasEmailAddresses.Value)
			{
				yield return new ValidationResult("The option --hasEmail can only be set to true.", [nameof(HasEmailAddresses)]);
			}

			if (Activities == false)
				yield return new ValidationResult("The option --activities can only be set to true.", [nameof(Activities)]);
			if (Color != null && !Regex.IsMatch(Color, @"\A#[0-9a-fA-F]{6}\z"))
				yield return new ValidationResult("The option --color must be a hexadecimal color in the format #RRGGBB.", [nameof(Color)]);
			if (HelpUrl != null && (!Uri.TryCreate(HelpUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
				yield return new ValidationResult("The option --helpUrl must be an absolute HTTP or HTTPS URL.", [nameof(HelpUrl)]);
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Update only the specified properties; omitted options are left unchanged. A boolean option without a value means true; use false to disable a setting where supported.");
			writer.WriteCodeBlock("pacx table update --schemaName new_contract --audit true --notes true", "PowerShell");
			writer.WriteCodeBlock("pacx table update --schemaName new_contract --color \"#336699\" --quickCreate false --primaryImage new_picture", "PowerShell");
			writer.WriteParagraph("Notes, feedback, activities, email, connections, queues and change tracking cannot be disabled once enabled. Changes are not published automatically. Updating the primary image and table together requires two requests and may partially succeed.");
		}
	}
}
