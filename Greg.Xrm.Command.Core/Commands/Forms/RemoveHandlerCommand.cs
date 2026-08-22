using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "removehandler", HelpText = "Removes a javascript event handler from the main form of a given table")]
	[Alias("form", "removehandler")]
	public class RemoveHandlerCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The name of the table to which the form belongs")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("library", "l", Order = 2, HelpText = "The name of the javascript webresource that contains the handler function (e.g. myprefix_/scripts/account.js)")]
		[Required]
		public string Library { get; set; } = string.Empty;

		[Option("function", "fn", Order = 3, HelpText = "The name of the function to remove, including its namespace (e.g. My.Account.onLoad)")]
		[Required]
		public string Function { get; set; } = string.Empty;

		[Option("event", "e", Order = 4, HelpText = "The form event to remove the handler from.", DefaultValue = FormEvent.OnLoad)]
		public FormEvent Event { get; set; } = FormEvent.OnLoad;

		[Option("field", "col", Order = 5, HelpText = "The logical name of the watched column. Required for (and only valid with) the OnChange event.")]
		public string? Field { get; set; }

		[Option("form", "f", Order = 6, HelpText = "The name of the form to update. It is required only if the table has more than one Main form.")]
		public string FormName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 7, HelpText = "The name of the solution that contains the table. If not provided, the default solution will be used")]
		public string? SolutionName { get; set; }

		[Option("output", "out", Order = 8, HelpText = "If specified, the command will export the original version of the form in the specified folder before applying any change. The folder must exist.")]
		public string? TempDir { get; set; }

		[Option("fast", "ft", Order = 9, HelpText = "Updates the formxml of the form directly instead of going through a temporary solution. Much faster; the platform still validates the formxml on update, but the all-or-nothing safety of a solution import is skipped.", DefaultValue = false)]
		public bool Fast { get; set; } = false;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Event == FormEvent.OnChange && string.IsNullOrWhiteSpace(Field))
			{
				yield return new ValidationResult("The --field argument is required for the OnChange event.", [nameof(Field)]);
			}

			if (Event != FormEvent.OnChange && !string.IsNullOrWhiteSpace(Field))
			{
				yield return new ValidationResult("The --field argument can only be used with the OnChange event.", [nameof(Field)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Counterpart of 'forms addhandler'. Removes a handler registration from the form definition, the same way deleting it in the form designer would.");

			writer.WriteParagraph("Remove an OnLoad handler from the main form of a table:");
			writer.WriteCodeBlock("pacx forms removehandler --table account --library myprefix_scripts.js --function My.Account.onLoad", "Powershell");

			writer.WriteParagraph("Remove an OnChange handler from a specific column:");
			writer.WriteCodeBlock("pacx forms removehandler -t account -l myprefix_scripts.js -fn My.Account.onNameChange -e OnChange --field name", "Powershell");

			writer.WriteParagraph("When the removed handler was the last one referencing the webresource library, the library is removed from the form as well. Event entries that remain empty are cleaned up, so the form definition ends up the same way the designer would leave it. If the handler is not registered, the command makes no change, so it is safe to run repeatedly.");

			writer.WriteParagraph("The command does not check whether the webresource still exists in the environment, so it can also be used to clean up registrations of webresources that have already been deleted.");

			writer.WriteParagraph("Use --output to save a backup of the original form before any change is applied. By default the form is updated through a temporary solution, use --fast to update the formxml directly instead, which takes seconds instead of minutes:");
			writer.WriteCodeBlock("pacx forms removehandler -t account -l myprefix_scripts.js -fn My.Account.onLoad --fast --output C:\\temp", "Powershell");

			writer.WriteParagraph("Please note: unless --fast is used, the command needs to create a temporary solution in the target environment. The temporary solution is deleted automatically once the operation is complete.");
		}
	}
}
