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

		[Option("name", "n", HelpText = "The display name of the table to be created.")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("plural", "p", HelpText = "The collection name of the table to be created.")]
		public string? DisplayCollectionName { get; set; }

		[Option("schemaName", "sn", HelpText = "Technical schema name of the table to be created. If not specified, it is inferred from the display name.")]
		public string? SchemaName { get; set; }

		[Option("description", "d", HelpText = "Meaningful description of the table contents/purpose.")]
		public string? Description { get; set; }

		[Option("ownership", "o", DefaultValue = OwnershipTypes.UserOwned, HelpText = "Defines if the table records can belong to an user or are organization-owned.")]
		public OwnershipTypes Ownership { get; set; } = OwnershipTypes.UserOwned;

		[Option("isActivity", "act", DefaultValue = false, HelpText = "Indicates whether the table is an activity or not.")]
		public bool IsActivity { get; set; } = false;

		[Option("offline", "off", DefaultValue = false, HelpText = "Indicates whether the table should be enabled for offline or not.")]
		public bool IsAvailableOffline { get; set; } = false;

		[Option("queue", "queue", DefaultValue = false, HelpText = "Indicates whether records of this table can be added to a queue or not.")]
		public bool IsValidForQueue { get; set; } = false;

		[Option("feedback", "fb", DefaultValue = false, HelpText = "Indicates whether user can provide feedbacks to records in this table or not.")]
		public bool HasFeedback { get; set; } = false;

		[Option("notes", DefaultValue = false, HelpText = "Indicates whether user can add notes and attachments to the current table or not.")]
		public bool HasNotes { get; set; } = false;

		[Option("audit", "a", DefaultValue = true, HelpText = "Indicates whether audit is enabled or not.")]
		public bool IsAuditEnabled { get; set; } = true;

		[Option("connection", "conn", DefaultValue = false, HelpText = "Indicates whether the current table can partecipate in connection relationships or not.")]
		public bool IsConnectionsEnabled { get; set; } = false;

		[Option("primaryAttributeName", "pan", HelpText = "The display name of the primary attribute for the table. If not specified, is used **Name**, unless it is required to be an autonumber. In that case, **Code** is used.")]
		public string? PrimaryAttributeDisplayName { get; set; }

		[Option("primaryAttributeSchemaName", "pas", HelpText = "The schema name of the primary attribute for the table. If not specified, it's inferred from the primary attribute name.")]
		public string? PrimaryAttributeSchemaName { get; set; }

		[Option("primaryAttributeDescription", "pad", HelpText = "A description for the primary attribute of the table.")]
		public string? PrimaryAttributeDescription { get; set; }

		[Option("primaryAttributeAutoNumberFormat", "paan", HelpText= "If the primary attribute should be an autonumber, indicates the format for the autonumber (https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/create-auto-number-attributes?view=op-9-1#autonumberformat-options).")]
		public string? PrimaryAttributeAutoNumber { get; set; }

		[Option("primaryAttributeRequiredLevel", "par", HelpText = "Requirement level for the primary attribute. If not specified, and autonumber, it's None, otherwise it's ApplicationRequired")]
		public AttributeRequiredLevel? PrimaryAttributeRequiredLevel { get; set; }

		[Option("primaryAttributeMaxLength", "palen", DefaultValue = 100, HelpText = "Max length of the primary attribute for the table.")]
		public int? PrimaryAttributeMaxLength { get; set; }


		[Option("solution", "s", HelpText = "The name of the solution where the table will be created. If not provided, the default solution will be used.")]
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
