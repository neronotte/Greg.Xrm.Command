using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.AiBuilder
{
	[Command("ai", "model", "list", HelpText = "List all AI Builder models with training status and accuracy.")]
	public class AiModelListCommand
	{
		[Option("format", "f", Order = 1, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("status", "s", Order = 2, HelpText = "Filter by training status: NotStarted, Training, Completed, Failed.")]
		public string? Status { get; set; }
	}

	[Command("ai", "model", "train", HelpText = "Trigger AI Builder model training from labeled data.")]
	public class AiModelTrainCommand
	{
		[Option("model-id", "m", Order = 1, Required = true, HelpText = "AI Builder model ID to train.")]
		public string ModelId { get; set; } = "";

		[Option("wait", Order = 2, HelpText = "Wait for training to complete (polling every 30s).")]
		public bool Wait { get; set; }
	}

	[Command("ai", "model", "publish", HelpText = "Publish a trained AI Builder model to an environment.")]
	public class AiModelPublishCommand
	{
		[Option("model-id", "m", Order = 1, Required = true, HelpText = "AI Builder model ID to publish.")]
		public string ModelId { get; set; } = "";

		[Option("dry-run", Order = 2, HelpText = "Show what would be published without actually publishing.")]
		public bool DryRun { get; set; }
	}

	[Command("ai", "form-processor", "configure", HelpText = "Configure form processing model (document type, fields, tables).")]
	public class AiFormProcessorConfigureCommand
	{
		[Option("model-id", "m", Order = 1, Required = true, HelpText = "Form processing model ID.")]
		public string ModelId { get; set; } = "";

		[Option("doc-type", "d", Order = 2, Required = true, HelpText = "Document type name.")]
		public string DocumentType { get; set; } = "";

		[Option("fields", "f", Order = 3, HelpText = "Comma-separated list of field names to extract.")]
		public string[]? Fields { get; set; }

		[Option("tables", "t", Order = 4, HelpText = "Comma-separated list of table names to extract.")]
		public string[]? Tables { get; set; }
	}
}
