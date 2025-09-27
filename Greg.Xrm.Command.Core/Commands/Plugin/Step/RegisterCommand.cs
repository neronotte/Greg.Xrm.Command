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

		[Option("force", "f", HelpText = "Registers a new step even if a step with the same configuration (PluginType, Message and Table) already exists. If not specified, the current step will be updated.", DefaultValue = false)]]
		public bool Force { get; set; } = false;


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
			// "plugin step register --assembly-name MyPluginAssembly --class MyPluginNamespace.MyPluginClass --message Create --entity account --stage PreOperation --mode Synchronous --name 'My Plugin Step' --unsecure-configuration 'Some config' --secure-configuration 'Some secure config' --filter-attributes name,accountnumber --rank 1"

			// For the --class argument, you can use the full name of the class, or just the class name if it's unique within the system.


		}
	}


}
