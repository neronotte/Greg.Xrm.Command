using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("custom-api", "create", HelpText = "Create a Custom API (Custom Action) in Dataverse.")]
	public class CustomApiCreateCommand
	{
		[Option("name", "n", Order = 1, HelpText = "Unique name of the Custom API.")]

		[Required]
		public string Name { get; set; } = "";

		[Option("display-name", Order = 2, HelpText = "Display name. Defaults to the unique name.")]
		public string? DisplayName { get; set; }

		[Option("description", Order = 3, HelpText = "Description of the Custom API.")]
		public string? Description { get; set; }

		[Option("input", Order = 4, HelpText = "Input parameter in format 'Type:Name' (e.g., 'String:Target'). Repeatable.")]
		public string[]? Inputs { get; set; }

		[Option("output", Order = 5, HelpText = "Output parameter in format 'Type:Name' (e.g., 'Entity:Result'). Repeatable.")]
		public string[]? Outputs { get; set; }

		[Option("binding-type", Order = 6, DefaultValue = "Global", HelpText = "Binding type: Global, Entity, EntityCollection.")]
		public string BindingType { get; set; } = "Global";

		[Option("entity", "e", Order = 7, HelpText = "Entity logical name for Entity/EntityCollection binding.")]
		public string? EntityLogicalName { get; set; }

		[Option("solution", "s", Order = 8, HelpText = "Solution unique name to add the Custom API to.")]
		public string? SolutionUniqueName { get; set; }

		[Option("execute-plugin", Order = 9, HelpText = "Plugin type name to execute this Custom API.")]
		public string? ExecutePluginTypeName { get; set; }

		[Option("is-function", Order = 10, HelpText = "Whether the Custom API is a function (read-only, no side effects).")]
		public bool IsFunction { get; set; }
	}

	[Command("custom-api", "list", HelpText = "List all Custom APIs in the current environment.")]
	public class CustomApiListCommand
	{
		[Option("format", "f", Order = 1, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("entity", "e", Order = 2, HelpText = "Filter by bound entity logical name.")]
		public string? EntityLogicalName { get; set; }
	}

	[Command("custom-api", "delete", HelpText = "Delete a Custom API from Dataverse.")]
	public class CustomApiDeleteCommand
	{
		[Option("name", "n", Order = 1, HelpText = "Unique name of the Custom API to delete.")]

		[Required]
		public string Name { get; set; } = "";
	}
}

