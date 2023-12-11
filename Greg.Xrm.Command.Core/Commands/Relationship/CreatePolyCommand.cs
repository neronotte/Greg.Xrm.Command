using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
    [Command("rel", "create", "poly", HelpText = "Creates a new many-to-one **polymorphic** relationship between Dataverse tables")]
	[Alias("rel", "create", "poli")]
	public class CreatePolyCommand
	{
		[Option("child", "c", "The child table (N side of the relationship)")]
		[Required]
		public string ChildTable { get; set; } = string.Empty;

		[Option("parents", "p", "A comma or pipe separated list of entities that will act as parent for the relationship")]
		[Required]
		public string Parents { get; set; } = string.Empty;

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




		[Option("lookupDisplayName", "ldn", "The display name of the lookup attribute.")]
		[Required]
		public string LookupAttributeDisplayName { get; set; } = string.Empty;

		[Option("lookupSchemaName", "lsn", "The schema name of the lookup attribute. If not specified, it is inferred by the display name.")]
		public string? LookupAttributeSchemaName { get; set; }

		[Option("requiredLevel", "r", HelpText = "The required level of the lookup attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;





		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this relationship.")]
		public string? SolutionName { get; set; }
	}
}
