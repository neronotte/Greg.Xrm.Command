using Greg.Xrm.Command.Parsing;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Create
{
    [Command("create", "table", HelpText = "Creates a new table")]
    public class CreateTableCommand
    {
        // pacx create table --name "My Table"
        // pacx create table --name "My Table" --plural "My Tables" --schemaName "new_mytable" --description "My Table Description" --ownership User --isActivity false --primaryAttributeName "Name" --primaryAttributeSchemaName "new_name"

        [Option("name", "n", IsRequired = true)]
        public string? DisplayName { get; set; }

        [Option("plural", "p")]
        public string? DisplayCollectionName { get; set; }

        [Option("schemaName", "sn")]
        public string? SchemaName { get; set; }

        [Option("description", "d")]
        public string? Description { get; set; }

        [Option("ownership", "o", DefaultValue = OwnershipTypes.UserOwned)]
        public OwnershipTypes Ownership { get; set; } = OwnershipTypes.UserOwned;

		[Option("isActivity", "act", DefaultValue = false)]
		public bool IsActivity { get; set; } = false;

		[Option("audit", "a", DefaultValue = true)]
		public bool IsAuditEnabled { get; set; } = true;

		[Option("primaryAttributeName", "pan")]
        public string? PrimaryAttributeDisplayName { get; set; }

        [Option("primaryAttributeSchemaName", "pas")]
        public string? PrimaryAttributeSchemaName { get; set; }

        [Option("primaryAttributeDescription", "pad")]
        public string? PrimaryAttributeDescription { get; set; }

        [Option("primaryAttributeAutoNumberFormat", "paan")]
        public string? PrimaryAttributeAutoNumber { get; set; }

        [Option("primaryAttributeRequiredLevel", "par")]
        public AttributeRequiredLevel? PrimaryAttributeRequiredLevel { get; set; }

        [Option("primaryAttributeMaxLength", "palen")]
        public int? PrimaryAttributeMaxLength { get; set; }


        [Option("solution", "s", HelpText = "The name of the solution where the table will be created")]
        public string? SolutionName { get; set; }
    }
}
