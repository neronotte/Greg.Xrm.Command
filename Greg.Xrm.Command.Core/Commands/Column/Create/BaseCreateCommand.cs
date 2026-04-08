using System.ComponentModel.DataAnnotations;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public abstract class BaseCreateCommand
	{
		[Option("table", "t", HelpText = "The name of the entity for which you want to create an attribute", Order = 0)]
		[Required]
		public string? EntityName { get; set; }

		[Option("name", "n", HelpText = "The display name of the attribute.", Order = 1)]
		[Required]
		public string? DisplayName { get; set; }

		[Option("schemaName", "sn", HelpText = "The schema name of the attribute.\nIf not specified, is deducted from the display name", Order = 3)]
		public string? SchemaName { get; set; }

		[Option("description", "d", HelpText = "The description of the attribute.", Order = 4)]
		public string? Description { get; set; }

		[Option("requiredLevel", "r", HelpText = "The required level of the attribute.", Order = 5)]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;


		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this attribute.", Order = 10000)]
		public string? SolutionName { get; set; }

		[Option("audit", "a", HelpText = "Indicates whether the attribute is enabled for auditing (default: true).")]
		public bool IsAuditEnabled { get; set; } = true;
	}
}
