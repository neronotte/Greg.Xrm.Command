using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;
using static Greg.Xrm.Command.Services.Plugin.PluginRegistrationToolkit;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	[Command("plugin", "step", "disable", HelpText = "Disables a plugin step registration. It's useful for debugging purposes: the step stays in the system but won't be executed.")]
	[Alias("plugin", "step", "deactivate")]
	public class DisableCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("class", "c", HelpText = "Name of the plugin type that executes when the step is triggered.")]
		public string PluginTypeName { get; set; } = string.Empty;

		[Option("message", "m", HelpText = "Message that triggers the step, e.g., Create, Update, Delete.")]
		public string MessageName { get; set; } = string.Empty;


		[Option("stage", "st", HelpText = "Pipeline stage when the step executes. Possible values: PreValidation (10), PreOperation (20), PostOperation (40)")]
		public Stage? Stage { get; set; }

		[Option("table", "t", HelpText = "Primary table for the step, e.g., account, contact. Leave empty for global messages (e.g. Recalculate).")]
		public string PrimaryEntityName { get; set; } = string.Empty;

		[Option("id", "id", HelpText = "The unique identifier of the plugin step to be removed.")]
		public Guid? StepId { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (StepId == null && (string.IsNullOrWhiteSpace(PluginTypeName) || string.IsNullOrWhiteSpace(MessageName) || Stage == null))
			{
				yield return new ValidationResult("Either StepId or both PluginTypeName, MessageName and Stage must be provided.", [nameof(StepId), nameof(PluginTypeName), nameof(MessageName), nameof(Stage)]);
			}

			if (StepId != null && !string.IsNullOrWhiteSpace(PluginTypeName))
			{
				yield return new ValidationResult("Cannot specify both StepId and PluginTypeName. Please provide only one of these parameters.", [nameof(StepId), nameof(PluginTypeName)]);
			}
		}



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("To disable a plugin step, you have two ways:");
			writer.WriteList(
			"By specifying the unique identifier of the step:",
			"By specifying a surrogate key that allows to identify the step to remove, made by the name of the plugin type, the message, and optionally the table");
			writer.WriteLine();

			writer.WriteCodeBlock(@"# Using the unique identifier of the step
pacx plugin step disable --id 3fa85f64-5717-4562-b3fc-2c963f66afa6			
			", "Powershell");
			writer.WriteLine();

			writer.WriteCodeBlock(@"# Using the alternative key
pacx plugin step disable --class Neronotte.Plugins.Account_OnPreCreate_ValidateFields --message Create --table account --stage PreOperation
			", "Powershell");
			writer.WriteLine();

			writer.WriteParagraph(@"Just like in the `register` command, for the `--class` argument, you can use the full name of the class, 
or just (the end of) the class name if it's unique within the system. 
E.g. if the actual plugin type name is **Neronotte.MyProject.Plugins.Account_OnPreCreate_ValidateFields**, the following are all valid options for the command: ");


			writer.WriteList(
				"`--class Neronotte.MyProject.Plugins.Account_OnPreCreate_ValidateFields` # fully qualified name",
				"`--class Account_OnPreCreate_ValidateFields` # just the plugin name",
				"`--class MyProject.Plugins.Account_OnPreCreate_ValidateFields` # unambiguos namespace + plugin name");
		}
	}
}
