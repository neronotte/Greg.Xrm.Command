using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Catalog
{
	[Command("catalog", "publish-item", HelpText = "Publish an item to the Dataverse Catalog for Business Events.")]
	public class CatalogPublishCommand
	{
		[Option("name", "n", Order = 1, Required = true, HelpText = "Name of the catalog item.")]
		public string Name { get; set; } = "";

		[Option("type", "t", Order = 2, DefaultValue = "BusinessEvent", HelpText = "Catalog item type: BusinessEvent, ApiDefinition.")]
		public string Type { get; set; } = "BusinessEvent";

		[Option("description", Order = 3, HelpText = "Description of the catalog item.")]
		public string? Description { get; set; }

		[Option("version", Order = 4, HelpText = "Version of the catalog item.")]
		public string? Version { get; set; } = "1.0.0";

		[Option("definition", "d", Order = 5, HelpText = "Path to the JSON/YAML definition file.")]
		public string? DefinitionPath { get; set; }

		[Option("dry-run", Order = 6, HelpText = "Show what would be published without actually publishing.")]
		public bool DryRun { get; set; }
	}
}
