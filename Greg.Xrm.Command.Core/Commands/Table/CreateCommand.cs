using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "create", HelpText = "Creates a new table in the dataverse environment that has previously been selected via `pacx auth select`")]
    [Alias("create", "table")]
	public class CreateCommand : ICanProvideUsageExample
    {
        // pacx create table --name "My Table"
        // pacx create table --name "My Table" --plural "My Tables" --schemaName "new_mytable" --description "My Table Description" --ownership User --isActivity false --primaryAttributeName "Name" --primaryAttributeSchemaName "new_name"

        [Option("name", "n")]
        [Required]
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

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to create a new table specifying a minimun set of information. You can simply type");
            writer.WriteCodeBlock("pacx table create --name \"My Table\"", "Command");
            writer.Write("to create a new table named \"My Table\" in the solution set as default via ").WriteCode("pacx solution setDefault").WriteLine(".");
            writer.Write("The table schema name will be generated automatically extrapolating only chars, numbers and underscores from the display name,")
                .Write("setting them lowercase, prefixed with the solution's publisher prefix.")
                .WriteLine();
            writer.Write("In this case, if the publisher prefix is ").WriteCode("greg").Write("), the generated schema name will be ").WriteCode("greg_mytable").WriteLine();
            writer.WriteLine();

		}
	}
}
