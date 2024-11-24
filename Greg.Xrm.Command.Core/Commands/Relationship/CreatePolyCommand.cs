using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
    [Command("rel", "poly", "create", HelpText = "Creates a new many-to-one **polymorphic** relationship between Dataverse tables")]
	[Alias("rel", "poli", "create")]
	[Alias("rel", "create", "poly")]
	[Alias("rel", "create", "poli")]
	public class CreatePolyCommand : ICanProvideUsageExample
	{
		[Option("child", "c", "The child table (N side of the relationship)")]
		[Required]
		public string ChildTable { get; set; } = string.Empty;

		[Option("parents", "p", "A comma or pipe separated list of entities that will act as parent for the relationship")]
		[Required]
		public string Parents { get; set; } = string.Empty;

		[Option("lookupDisplayName", "ldn", "The display name of the lookup attribute.")]
		[Required]
		public string LookupAttributeDisplayName { get; set; } = string.Empty;

		[Option("lookupSchemaName", "lsn", "The schema name of the lookup attribute. If not specified, it is inferred by the display name.")]
		public string? LookupAttributeSchemaName { get; set; }

		[Option("requiredLevel", "r", HelpText = "The required level of the lookup attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;



		[Option("relNameSuffix", "suff", "The suffix to append to the relationship name. If not provided, will be set equal to the display name of the lookup attribute (only letters, numbers, or underscores, lowercase).")]
		public string? RelationshipNameSuffix { get; set; }



		[Option("cascadeAssign", "caass", "The behavior to apply to child records when the parent record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign { get; set; }

		[Option("cascadeArchive", "caarc", "The behavior to apply to child records when the parent record is archived\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive { get; set; }

		[Option("cascadeShare", "cas", "The behavior to apply to child records when the parent record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare { get; set; }

		[Option("cascadeUnshare", "cau", "The behavior to apply to child records when the parent record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare { get; set; }

		[Option("cascadeDelete", "cad", "The behavior to apply when the parent record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge", "cam", "The behavior to apply to child records when the parent record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge { get; set; }

		[Option("cascadeReparent", "car", "The behavior to apply to child records when the parent record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeReparent { get; set; }






		[Option("menuBehavior", "m", "Indicates how the child entity is displayed in the parent navbar", DefaultValue = AssociatedMenuBehavior.DoNotDisplay)]
		public AssociatedMenuBehavior MenuBehavior { get; set; } = AssociatedMenuBehavior.DoNotDisplay;

		[Option("menuLabel", "ml", "Associated menu label. To be specified only if the menuBehavior arg is set to UseLabel", DefaultValue = null)]
		public string? MenuLabel { get; set; }

		[Option("menuGroup", "mg", "Associated menu group. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = AssociatedMenuGroup.Details)]
		public AssociatedMenuGroup MenuGroup { get; set; } = AssociatedMenuGroup.Details;

		[Option("menuOrder", "mo", "Associated menu order. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = 10000)]
		public int MenuOrder { get; set; } = 10000;





		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this relationship.")]
		public string? SolutionName { get; set; }





		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command allows to easily create polymorphic relationships between Dataverse tables, a feature that is **not yet available via UI**, but it's fully supported via API.");

			writer.WriteParagraph("> **Please note** that this command assumes that the relationship behavior is equal for all the parent tables. If you need to specify different behaviors, you need to create the relationship manually via API, or you can change it later via UI");

			writer.WriteParagraph("All you need to pass is:");
			writer.WriteLine("- The schema name of the table that will contain the lookup column (the **child** table)");
			writer.WriteLine("- The list schema names of the table that will be referenced by the lookup column (the **parents** table), separated by comma or pipe");
			writer.WriteLine("- The display name of the lookup attribute (the **lookupDisplayName** arg)");
			writer.WriteLine();

			writer.WriteParagraph("all other info, if not passed explicitly, is automatically inferred from the provided arguments.");

			writer.WriteCodeBlock("pacx rel create poly -ldn \"Referenced By\" -c custom_table --p \"custom_parent1,custom_parent2,custom_parent3\" ", "PowerShell");

			writer.WriteLine("The following default logic applies when a given argument is not explicitly passed to the command:").WriteLine();

			writer.WriteLine("- The schema name of the lookup attribute is inferred from the display name. Only chars, numbers and underscores are extracted and put lowercase. The schema name is obtained concatenating the publisher prefix (from the selected solution), the \"cleaned\" display name, and the string \"id\".");
			writer.WriteLine("- The relationship name is obtained concatenating the schema name of the lookup attribute, the schema name of the child table, and a suffix, all lowercase, separated by underscores.");
			writer.WriteLine("- The relationship name suffix is obtained from the display name of the lookup attribute, by removing all chars that are not letters, numbers or underscores, and putting everything lowercase.");
			writer.WriteLine("- The menu behavior is set to \"DoNotDisplay\" for all the relationships.");
			writer.WriteLine("- The lookup behavior is set to Referential / Restrict Delete for all the relationships.");
			writer.WriteLine("- The lookup attribute is set as optional.");
		}
	}
}
