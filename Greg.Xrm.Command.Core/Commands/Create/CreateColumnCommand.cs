using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Create
{
	[Command("create", "column")]
	public class CreateColumnCommand
	{
		[Option("table", "t", IsRequired = true, HelpText = "The name of the entity for which you want to create an attribute")]
		public string? EntityName { get; set; }

		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this attribute.")]
		public string? SolutionName { get; set; }

		[Option("name", "n", IsRequired = true, HelpText = "The display name of the attribute.")]
		public string? DisplayName { get; set; }

		[Option("schemaName", "sn", HelpText = "The schema name of the attribute. If not specified, is deducted from the display name")]
		public string? SchemaName { get; set; }

		[Option("description", "d", HelpText = "The description of the attribute.")]
		public string? Description { get; set; }

		[Option("type", "at", HelpText = "The type of the attribute. Default: string")]
		public AttributeTypeCode AttributeType { get; set; } = AttributeTypeCode.String;

		[Option("stringFormat", "f", HelpText = "The format of the string attribute (default: Text).")]
		public StringFormat StringFormat { get; set; } = StringFormat.Text;

		[Option("requiredLevel", "r", HelpText = "The required level of the attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;

		[Option("len", "l", HelpText = "The maximum length for string attribute.")]
		public int? MaxLength { get; set; }

		[Option("autoNumber", "an", HelpText = "In case of autonumber field, the autonumber format to apply.")]
		public string? AutoNumber { get; set; }

		[Option("audit", "a", HelpText = "Indicates whether the attribute is enabled for auditing (default: true).")]
		public bool IsAuditEnabled { get; set; } = true;

		[Option("options", "o", HelpText = "The list of options for the attribute, as a single string separated by comma or pipe. Values will be automatically generated")]
		public string? Options { get; internal set; }
	}
}
