using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "create", "nn", HelpText = "Creates a many-to-many relationship between two tables")]
	public class CreateNNCommand : ICanProvideUsageExample
	{
		[Option("table1", "t1", "The first table (schema name)")]
		[Required]
		public string Table1 { get; set; } = string.Empty;

		[Option("table2", "t2", "The second table (schema name)")]
		[Required]
        public string Table2 { get; set; } = string.Empty;


		[Option("schemaName", "sn", HelpText = "The name of the table that manages the intersection between the two tables.\nIf not specified, is calculated concatenating the schema names of the two entities.")]
		public string? IntersectSchemaName { get; set; }

		[Option("suffix", "sns", HelpText = "The suffix to be appended to the schema name of the intersection table.\nIs considered only if --schemaName is not provided.")]
		public string? IntersectSchemaNameSuffix { get; set; }


		[Option("menuBehavior1", "m1", "Indicates how the table1 is displayed in the table2 navbar", DefaultValue = AssociatedMenuBehavior.DoNotDisplay)]
		public AssociatedMenuBehavior MenuBehavior1 { get; set; } = AssociatedMenuBehavior.DoNotDisplay;

		[Option("menuLabel1", "ml1", "Indicates the menu label used to display table1 records in table2 navbar. To be specified only if the menuBehavior arg is set to UseLabel", DefaultValue = null)]
		public string? MenuLabel1 { get; set; }

		[Option("menuGroup1", "mg1", "Indicates the menu group that will contain table1 label in table2 navbar. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = AssociatedMenuGroup.Details)]
		public AssociatedMenuGroup MenuGroup1 { get; set; } = AssociatedMenuGroup.Details;

		[Option("menuOrder1", "mo1", "Indicates the sequence used to display table1 label in table2 navbar. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = 10000)]
		public int MenuOrder1 { get; set; } = 10000;




		[Option("menuBehavior2", "m2", "Indicates how the table2 entity is displayed in the table1 navbar", DefaultValue = AssociatedMenuBehavior.DoNotDisplay)]
		public AssociatedMenuBehavior MenuBehavior2 { get; set; } = AssociatedMenuBehavior.DoNotDisplay;

		[Option("menuLabel2", "ml2", "Indicates the menu label used to display table2 records in table1 navbar. To be specified only if the menuBehavior arg is set to UseLabel", DefaultValue = null)]
		public string? MenuLabel2 { get; set; }

		[Option("menuGroup2", "mg2", "Indicates the menu group that will contain table2 label in table1 navbar. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = AssociatedMenuGroup.Details)]
		public AssociatedMenuGroup MenuGroup2 { get; set; } = AssociatedMenuGroup.Details;

		[Option("menuOrder2", "mo2", "Indicates the sequence used to display table2 label in table1 navbar. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = 10000)]
		public int MenuOrder2 { get; set; } = 10000;


		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this relationship.")]
		public string? SolutionName { get; set; }

		[Option("explicit", "e", HelpText = "Indicates whether the relationship is an explicit or implicit relationship.", DefaultValue = false)]
		public bool Explicit { get; set; } = false;



		[Option("name", "n", HelpText = "Only for explicit relationships. The display name of the intersect table.")]
		public string? DisplayName { get; set; }

		[Option("plural", "p", HelpText = "Only for explicit relationships. The plural display name of the intersect table.")]
		public string? DisplayCollectionName { get; set; }

		[Option("description", "d", HelpText = "Only for explicit relationships. The description of the intersect table.")]
		public string? Description { get; set; }

		[Option("ownership", "o", HelpText = "Only for explicit relationships. The ownership of the intersect table", DefaultValue = OwnershipTypes.UserOwned)]
		public OwnershipTypes Ownership { get; set; } = OwnershipTypes.UserOwned;

		[Option("audit", "a", HelpText = "Only for explicit relationships. Indicates whether audit is enabled", DefaultValue = false)]
		public bool IsAuditEnabled { get; set; } = false;





		[Option("primaryAttributeName", "pan", HelpText = "Only for explicit relationships. The display name of the primary attribute of the intersect table.", DefaultValue = "Code")]
		public string PrimaryAttributeDisplayName { get; set; } = "Code";

		[Option("primaryAttributeSchemaName", "pas", HelpText = "Only for explicit relationships. The schema name of the primary attribute of the intersect table. If not specified, is deducted from the display name")]
		public string? PrimaryAttributeSchemaName { get; set; }

		[Option("primaryAttributeDescription", "pad", HelpText = "Only for explicit relationships. The description of the primary attribute of the intersect table.")]
		public string? PrimaryAttributeDescription { get; set; }

		[Option("primaryAttributeAutoNumberFormat", "paan", HelpText = "Only for explicit relationships. If not specified, it is assumed as {initial of table1}{initial of table2}-{SEQNUM:10}.\nTo generate a simple text field instead, pass \"\".")]
		public string? PrimaryAttributeAutoNumber { get; set; }

		[Option("primaryAttributeRequiredLevel", "par", HelpText = "Only for explicit relationships. Indicates whether the primary attribute of the intersect table is required or not.", DefaultValue = AttributeRequiredLevel.None)]
		public AttributeRequiredLevel PrimaryAttributeRequiredLevel { get; set; } = AttributeRequiredLevel.None;

		[Option("primaryAttributeMaxLength", "palen", HelpText = "Only for explicit relationship. Indicates the len of the primary attribute. Is set to 20 in case of autonumber field, 100 in case of text field.")]
		public int? PrimaryAttributeMaxLength { get; set; }






		[Option("cascadeAssign1", "caass1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign1 { get; set; }

		[Option("cascadeArchive1", "caarc1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is archived\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive1 { get; set; }

		[Option("cascadeShare1", "cas1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare1 { get; set; }

		[Option("cascadeUnshare1", "cau1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare1 { get; set; }

		[Option("cascadeDelete1", "cad1", "Only for explicit relationship. The behavior to apply to relationship table when the table1 record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete1 { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge1", "cam1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge1 { get; set; }

		[Option("cascadeReparent1", "car1", "Only for explicit relationship. The behavior to apply to relationship table records when the table1 record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeReparent1 { get; set; }





		[Option("cascadeAssign2", "caass2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign2 { get; set; }

		[Option("cascadeArchive2", "caarc2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is archived\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive2 { get; set; }

		[Option("cascadeShare2", "cas2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare2 { get; set; }

		[Option("cascadeUnshare2", "cau2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare2 { get; set; }

		[Option("cascadeDelete2", "cad2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete2 { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge2", "cam2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge2 { get; set; }

		[Option("cascadeReparent2", "car2", "Only for explicit relationship. The behavior to apply to relationship table records when the table2 record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeReparent2 { get; set; }






		[Option("lookupDisplayName1", "ldn1", "Only for explicit relationship. The display name of the lookup attribute vs table1. If not specified, the display name of the parent table is taken as default.")]
		public string? LookupAttributeDisplayName1 { get; set; }

		[Option("lookupSchemaName1", "lsn1", "Only for explicit relationship. The schema name of the lookup attribute vs table1. If not specified, the")]
		public string? LookupAttributeSchemaName1 { get; set; }

		[Option("requiredLevel1", "r1", HelpText = "Only for explicit relationship. The required level of the lookup attribute vs table1.", DefaultValue = AttributeRequiredLevel.SystemRequired)]
		public AttributeRequiredLevel LookupAttributeRequiredLevel1 { get; set; } = AttributeRequiredLevel.SystemRequired;




		[Option("lookupDisplayName2", "ldn2", "Only for explicit relationships. The display name of the lookup attribute vs table2. If not specified, the display name of the parent table is taken as default.")]
		public string? LookupAttributeDisplayName2 { get; set; }

		[Option("lookupSchemaName2", "lsn2", "Only for explicit relationships. The schema name of the lookup attribute vs table2. If not specified, the")]
		public string? LookupAttributeSchemaName2 { get; set; }

		[Option("requiredLevel2", "r2", HelpText = "Only for explicit relationships. The required level of the lookup attribute vs table2.", DefaultValue = AttributeRequiredLevel.SystemRequired)]
		public AttributeRequiredLevel LookupAttributeRequiredLevel2 { get; set; } = AttributeRequiredLevel.SystemRequired;






		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command reduces to almost 0 the effort required to create implicit or explicit many-to-many relationships between Dataverse tables.");

			writer.WriteParagraph("An **implicit** many-to-many relationship is the classic many-to-many relationship, where the relationship table is created automatically by Dataverse when you create the relationship between two tables.");
			
			writer.WriteLine("An **explicit** many-to-many relationship is a relationship where you create the relationship table manually, and then you create the relationship between the relationship table and the two tables you want to relate.");
			writer.WriteLine("Explicit many-to-many relationships can have a lot of advantages (e.g. you can add additional attributes in the relationship table), and tipically have a primary column of type autonumber.");
			writer.WriteLine("If you don't specify explicit information for the relationship talbe, the command will create it assuming that:");
			writer.WriteLine();
			writer.WriteLine("- The entity schema name is the concatenation between the schema names of the two tables to relate (publisher prefix removed automatically)");
			writer.WriteLine("- The display name is the concatenation between the display names of the two tables to relate, separated by dash -");
			writer.Write("- The primary column is an autonumber with format ").WriteCode("{initial of table1}{initial of table2}-{SEQNUM:10}");
			writer.WriteLine(" (e.g. if the two tables are Account and Contact, the primary column value will be `AC-0000000001`)");
			writer.WriteLine("- Audit for the intersection table is disabled");
			writer.WriteLine("- The intersection table ownership is `UserOwned`");
			writer.WriteLine("- The primary column is not required (it's autogenerated)");
			writer.WriteLine("- The lookup attributes against the two tables have display name equal to the display name of each table");
			writer.WriteLine("- The lookup attributes against the two tables have schema name equal to the schema name of each table + `id` suffix");
			writer.WriteLine("- The lookup attributes against the two tables have Referential/Restrict behavior");
			writer.WriteLine();

			writer.WriteParagraph("Thus, if you have two tables `account` and `contact`, and you want to create a many-to-many relationship between them, you can simply type:");

			writer.WriteCodeBlockStart("PowerShell");
			writer.WriteLine("pacx rel create nn -t1 account -t2 contact");
			writer.WriteLine(" --- or ---");
			writer.WriteLine("pacx rel create nn -t1 account -t2 contact --explicit");
			writer.WriteCodeBlockEnd();

			writer.WriteParagraph("All other arguments are optional.");

			writer.WriteLine("If you are creating an **implicit** many-to-many relationship, the schema name of the relationship is built concatenating the schema names of the two tables to relate (publisher prefix removed automatically).");
			writer.WriteLine("There are some cases where this concatenation is not enough to create a unique schema name for the relationship table.");
			writer.WriteLine("In this case, you can use the `--suffix` option to specify a suffix to apply on the relationship schema name, or you can use `--schemaName` option to explicitly indicate a custom schema name for the relationship.");
			writer.WriteLine();
		}
	}
}
