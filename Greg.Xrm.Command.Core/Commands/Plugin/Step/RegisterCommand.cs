using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;
using static Greg.Xrm.Command.Services.Plugin.PluginRegistrationToolkit;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	[Command("plugin", "step", "register", HelpText = "Registers a plugin step.")]
	public class RegisterCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("class", "c", HelpText = "Name of the plugin type that executes when the step is triggered.")]
		[Required]
		public string PluginTypeName { get; set; } = string.Empty;

		[Option("message", "m", HelpText = "Message that triggers the step, e.g., Create, Update, Delete.")]
		[Required]
		public string MessageName { get; set; } = string.Empty;

		[Option("table", "t", HelpText = "Primary table for the step, e.g., account, contact. Leave empty for global messages (e.g. Recalculate).")]
		public string PrimaryEntityName { get; set; } = string.Empty;

		[Option("filteringAttributes", "fa", HelpText = "Comma (,) separated list of columns acting as filtering attributes for the message")]
		public string FilteringAttributes { get; set; } = string.Empty;

		[Option("name", "n", HelpText = "Name of the plugin step. If not specified, will be defined automatically by the platform")]
		public string? Name { get; set; }

		[Option("user", "u", HelpText = "Specify this argument if you want to run the step in a specific user context. Provide the User's GUID. Leave empty to run the plugin in the calling user context.")]
		public Guid? UserId { get; set; }

		[Option("order", "o", HelpText = "Execution order of the step. Lower numbers execute first.", DefaultValue = 1)]
		public int ExecutionOrder { get; set; } = 1;

		[Option("stage", "st", HelpText = "Pipeline stage when the step executes. Possible values: PreValidation (10), PreOperation (20), PostOperation (40)", DefaultValue = Stage.PreOperation)]
		public Stage Stage { get; set; } = Stage.PreOperation;

		[Option("mode", "md", HelpText = "Execution mode of the step. Possible values: Sync, Async", DefaultValue = Mode.Sync)]
		public Mode Mode { get; set; } = Mode.Sync;

		[Option("deployment", "dep", HelpText = "Deployment type", DefaultValue = Deployment.ServerOnly)]
		public Deployment Deployment { get; set; } = Deployment.ServerOnly;

		[Option("description", "d", HelpText = "Description of the plugin step.")]
		public string? Description { get; set; }

		[Option("unsecureConfig", "uc", HelpText = "Unsecure configuration string for the plugin step.")]
		public string? UnsecureConfiguration { get; set; }

		[Option("secureConfig", "sc", HelpText = "Secure configuration string for the plugin step.")]
		public string? SecureConfiguration { get; set; }

		[Option("solution", "s", HelpText = "The name of the solution where step must be added (in case of creation). If not provided, the default solution will be used.")]
		public string? SolutionName { get; set; }

		[Option("preImage", "preim", HelpText = "Indicates whether a PreImage must be registered on the step.", DefaultValue = false)]
		public bool PreImage { get; set; } = false;

		[Option("preImageName", "preimn", HelpText = "Name of the PreImage. If not specified, will be set automatically as <table name>_pre")]
		public string? PreImageName { get; set; }

		[Option("postImage", "postim", HelpText = "Indicates whether a PostImage must be registered on the step.", DefaultValue = false)]
		public bool PostImage { get; set; } = false;

		[Option("postImageName", "postimn", HelpText = "Name of the PreImage. If not specified, will be set automatically as <table name>_pre")]
		public string? PostImageName { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (PreImage && string.IsNullOrWhiteSpace(PrimaryEntityName))
				yield return new ValidationResult("PreImage cannot be registered for global messages. Please specify a PrimaryEntityName.", [nameof(PreImage), nameof(PrimaryEntityName)]);
			if (PostImage && string.IsNullOrWhiteSpace(PrimaryEntityName))
				yield return new ValidationResult("PostImage cannot be registered for global messages. Please specify a PrimaryEntityName.", [nameof(PostImage), nameof(PrimaryEntityName)]);


			if (!string.IsNullOrWhiteSpace(PreImageName) && !PreImage)
				yield return new ValidationResult("PreImageName is specified but PreImage is not set to true.", [nameof(PreImageName), nameof(PreImage)]);
			if (!string.IsNullOrWhiteSpace(PostImageName) && !PostImage)
				yield return new ValidationResult("PostImageName is specified but PostImage is not set to true.", [nameof(PostImageName), nameof(PostImage)]);

		}



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("To register a plugin step, you need to provide at minimum the plugin class name and message name. Other parameters have sensible defaults or are optional.");
			
			writer.WriteTitle3("Minimum Required Parameters");
			writer.WriteList(
				"`--class` (or `-c`): Name of the plugin type that executes when the step is triggered",
				"`--message` (or `-m`): Message that triggers the step (e.g., Create, Update, Delete)");
			writer.WriteLine();

			writer.WriteTitle3("Default Values and Conventions");
			writer.WriteParagraph("When not specified, the following defaults are applied:");
			writer.WriteList(
				"`--stage`: PreOperation (20) - the step executes in the Pre-Operation stage",
				"`--mode`: Sync (0) - the step executes synchronously",
				"`--deployment`: ServerOnly (0) - the step only runs on the server",
				"`--order`: 1 - execution order within the stage",
				"`--solution`: Uses the current default solution from settings",
				"Step name: Automatically generated by the platform if not specified",
				"`--preImageName`: Automatically set as `<tablename>_pre` when `--preImage` is true",
				"`--postImageName`: Automatically set as `<tablename>_post` when `--postImage` is true");
			writer.WriteLine();

			writer.WriteTitle3("Plugin Type Name Resolution");
			writer.WriteParagraph(@"For the `--class` argument, you can use the full name of the class, 
or just (the end of) the class name if it's unique within the system. 
The system will perform a fuzzy search and display matches if multiple types are found.");
			
			writer.WriteParagraph("Valid options for a plugin type named **Neronotte.MyProject.Plugins.Account_OnPreCreate_ValidateFields**:");
			writer.WriteList(
				"`--class Neronotte.MyProject.Plugins.Account_OnPreCreate_ValidateFields` # fully qualified name",
				"`--class Account_OnPreCreate_ValidateFields` # just the plugin name",
				"`--class MyProject.Plugins.Account_OnPreCreate_ValidateFields` # unambiguous namespace + plugin name");
			writer.WriteLine();

			writer.WriteTitle3("Message and Table Validation");
			writer.WriteParagraph(@"The system validates that the specified message can be registered for the given table. 
If a message requires a specific table and none is provided, or if the table is not valid for the message, 
the command will fail with a list of valid tables for that message.");
			writer.WriteLine();

			writer.WriteTitle3("Usage Examples");
			
			writer.WriteParagraph("**Basic registration** (minimum parameters):");
			writer.WriteCodeBlock(@"# Register a plugin step with minimum parameters
pacx plugin step register --class Account_OnPreCreate_ValidateFields --message Create", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Complete registration** with all common options:");
			writer.WriteCodeBlock(@"# Register a plugin step with full configuration
pacx plugin step register \
  --class Neronotte.MyProject.Plugins.Account_OnPreCreate_ValidateFields \
  --message Update \
  --table account \
  --stage PreOperation \
  --mode Sync \
  --order 10 \
  --filteringAttributes ""name,accountnumber"" \
  --description ""Validates account fields before update"" \
  --unsecureConfig ""validateEmptyFields=true"" \
  --solution MyCustomSolution", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Registration with images** (using automatic naming):");
			writer.WriteCodeBlock(@"# Register with Pre and Post images - names will be auto-generated as account_pre and account_post
pacx plugin step register \
  --class Account_OnUpdate_TrackChanges \
  --message Update \
  --table account \
  --preImage \
  --postImage", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Registration with custom image names:**");
			writer.WriteCodeBlock(@"# Register with Pre and Post images using custom names
pacx plugin step register \
  --class Account_OnUpdate_TrackChanges \
  --message Update \
  --table account \
  --preImage \
  --preImageName ""account_before"" \
  --postImage \
  --postImageName ""account_after""", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Global message registration** (no table specified):");
			writer.WriteCodeBlock(@"# Register for a global message like Recalculate
pacx plugin step register --class GlobalRecalculate_Handler --message Recalculate", "powershell");
			writer.WriteLine();

			writer.WriteTitle3("Important Notes");
			writer.WriteList(
				"The plugin assembly must already be registered in the system before registering steps (use `pacx plugin push command`)",
				"If the solution is managed, the command will fail - use an unmanaged solution",
				"PreImage and PostImage can only be registered for entity-specific messages (not global messages)",
				"Image names are optional - if not provided, they default to `<tablename>_pre` and `<tablename>_post`",
				"The system will automatically add the registered step to the specified solution",
				"Filtering attributes help optimize performance by only triggering the plugin when specific fields change");
		}
	}


}
