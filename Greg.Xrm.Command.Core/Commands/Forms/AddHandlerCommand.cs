using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Forms
{
	[Command("forms", "addhandler", HelpText = "Registers a javascript event handler on the main form of a given table")]
	[Alias("form", "addhandler")]
	public class AddHandlerCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("table", "t", Order = 1, HelpText = "The name of the table to which the form belongs")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("library", "l", Order = 2, HelpText = "The name of the javascript webresource that contains the handler function (e.g. myprefix_/scripts/account.js)")]
		[Required]
		public string Library { get; set; } = string.Empty;

		[Option("function", "fn", Order = 3, HelpText = "The name of the function to call, including its namespace (e.g. My.Account.onLoad)")]
		[Required]
		public string Function { get; set; } = string.Empty;

		[Option("event", "e", Order = 4, HelpText = "The form event to attach the handler to.", DefaultValue = FormEvent.OnLoad)]
		public FormEvent Event { get; set; } = FormEvent.OnLoad;

		[Option("field", "col", Order = 5, HelpText = "The logical name of the column to watch. Required for (and only valid with) the OnChange event.")]
		public string? Field { get; set; }

		[Option("passContext", "ctx", Order = 6, HelpText = "Whether the execution context is passed as first parameter to the handler.", DefaultValue = true)]
		public bool PassExecutionContext { get; set; } = true;

		[Option("form", "f", Order = 7, HelpText = "The name of the form to update. It is required only if the table has more than one Main form.")]
		public string FormName { get; set; } = string.Empty;

		[Option("solution", "s", Order = 8, HelpText = "The name of the solution that contains the table. If not provided, the default solution will be used")]
		public string? SolutionName { get; set; }

		[Option("output", "out", Order = 9, HelpText = "If specified, the command will export the original version of the form in the specified folder before applying any change. The folder must exist.")]
		public string? TempDir { get; set; }

		[Option("fast", "ft", Order = 10, HelpText = "Updates the formxml of the form directly instead of going through a temporary solution. Much faster; the platform still validates the formxml on update, but the all-or-nothing safety of a solution import is skipped.", DefaultValue = false)]
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
			writer.WriteParagraph("Registering event handlers via the form designer is a manual, per-form activity. This command writes the same formLibraries and events entries into the form definition that the designer would, so the registration stays visible (and editable) in the designer afterwards.");

			writer.WriteParagraph("Attach an OnLoad handler to the main form of a table:");
			writer.WriteCodeBlock("pacx forms addhandler --table account --library myprefix_scripts.js --function My.Account.onLoad", "Powershell");

			writer.WriteParagraph("Attach an OnChange handler to a specific column:");
			writer.WriteCodeBlock("pacx forms addhandler -t account -l myprefix_scripts.js -fn My.Account.onNameChange -e OnChange --field name", "Powershell");

			writer.WriteParagraph("If the webresource library is not yet referenced by the form, it is added automatically. If the same function of the same library is already registered on the event, the command makes no change (only the passExecutionContext setting is updated when it differs), so it is safe to run repeatedly (e.g. from a setup script).");

			writer.WriteParagraph("Use --output to save a backup of the original form before any change is applied, e.g. when running against forms you care about:");
			writer.WriteCodeBlock("pacx forms addhandler -t account -l myprefix_scripts.js -fn My.Account.onLoad --output C:\\temp", "Powershell");

			writer.WriteParagraph("By default the form is updated through a temporary solution, so the change goes through the all-or-nothing validation of a solution import. Use --fast to update the formxml of the form directly instead, which takes seconds instead of minutes:");
			writer.WriteCodeBlock("pacx forms addhandler -t account -l myprefix_scripts.js -fn My.Account.onLoad --fast", "Powershell");

			writer.WriteParagraph("Please note: to update the form the command needs to create a temporary solution in the target environment. The temporary solution is deleted automatically once the operation is complete.");
		}
	}

	public enum FormEvent
	{
		OnLoad,
		OnSave,
		OnChange
	}
}
