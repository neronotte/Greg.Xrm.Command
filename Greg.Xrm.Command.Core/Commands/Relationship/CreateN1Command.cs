using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "create", "n1", HelpText = "Creates a new many-to-one relationship between two tables")]
	public class CreateN1Command
	{
		[Option("child", "c", Order = 1, HelpText ="The child table (N side of the relationship)")]
		[Required]
		public string ChildTable { get; set; } = string.Empty;

		[Option("parent", "p", Order = 2, HelpText ="The parent table (1 side of the relationship)")]
		[Required]
		public string ParentTable { get; set; } = string.Empty;

		[Option("relName", "rn", Order = 4, HelpText = "The name of the relationship. If not provided, the relationship name will be created\nconcatenating the names of the child and the parent table, with a suffix (if specified).")]
		public string? RelationshipName { get; set; }

		[Option("relNameSuffix", "suff", Order = 3, HelpText = "The suffix to append to the relationship name. If not provided, the relationship name will contain only\nthe concatenated names of the child and the parent table.")]
		public string? RelationshipNameSuffix { get; set; }







		[Option("lookupDisplayName", "ldn", Order = 4, HelpText = "The display name of the lookup attribute. If not specified, the display name of the parent table is taken as default.")]
		public string? LookupAttributeDisplayName { get; set; }

		[Option("lookupSchemaName", "lsn", Order = 5, HelpText = "The schema name of the lookup attribute. If not specified, it is inferred by the display name.")]
		public string? LookupAttributeSchemaName { get; set; }

		[Option("requiredLevel", "r", Order = 6, HelpText = "The required level of the lookup attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;



		[Option("cascadeAssign", "caass", Order = 15, HelpText = "The behavior to apply to child records when the parent record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign { get; set; }

		[Option("cascadeArchive", "caarc", Order = 16, HelpText = "The behavior to apply to child records when the parent record is archived\n(not available via UI)\n(default: NoCascade)",SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive { get; set; }

		[Option("cascadeShare", "cas", Order = 17, HelpText = "The behavior to apply to child records when the parent record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare { get; set; }

		[Option("cascadeUnshare", "cau", Order = 18, HelpText = "The behavior to apply to child records when the parent record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare { get; set; }

		[Option("cascadeDelete", "cad", Order = 19, HelpText = "The behavior to apply when the parent record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge", "cam", Order = 20, HelpText = "The behavior to apply to child records when the parent record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge { get; set; }

		[Option("cascadeReparent", "car", Order = 21, HelpText = "The behavior to apply to child records when the parent record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp =true)]
		public CascadeType? CascadeReparent { get; set; }






		[Option("menuBehavior", "m", Order = 32, HelpText = "Indicates how the child entity is displayed in the parent navbar", DefaultValue = AssociatedMenuBehavior.DoNotDisplay)]
		public AssociatedMenuBehavior MenuBehavior { get; set; } = AssociatedMenuBehavior.DoNotDisplay;

		[Option("menuLabel", "ml", Order = 33, HelpText= "Associated menu label. To be specified only if the menuBehavior arg is set to UseLabel", DefaultValue = null)]
		public string? MenuLabel { get; set; }

		[Option("menuGroup", "mg", Order = 34, HelpText = "Associated menu group. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = AssociatedMenuGroup.Details)]
		public AssociatedMenuGroup MenuGroup { get; set; } = AssociatedMenuGroup.Details;

		[Option("menuOrder", "mo", Order = 35, HelpText = "Associated menu order. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = 10000)]
		public int MenuOrder { get; set; } = 10000;





		[Option("solution", "s", Order = 100, HelpText = "The name of the unmanaged solution to which you want to add this relationship.")]
		public string? SolutionName { get; set; }

	}
}
