using Microsoft.Xrm.Sdk.Metadata;
using System.Runtime.Serialization;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "create", "n1", HelpText = "Creates a new N-1 relationship")]
	public class CreateN1Command
	{
		[Option("child", "c", true, "The child table (N side of the relationship)")]
		public string? ChildTable { get; set; }

		[Option("parent", "p", true, "The parent table (1 side of the relationship)")]
		public string? ParentTable { get; set; }

		[Option("relName", "rn", false, "The name of the relationship. If not provided, the relationship name will be created\nconcatenating the names of the child and the parent table, with a suffix (if specified).")]
		public string? RelationshipName { get; set; }

		[Option("relNameSuffix", "suff", false, "The suffix to append to the relationship name. If not provided, the relationship name will contain only\nthe concatenated names of the child and the parent table.")]
		public string? RelationshipNameSuffix { get; set; }







		[Option("cascadeAssign", "caass", false, "The behavior to apply to child records when the parent record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp = true)]
		public CascadeType CascadeAssign { get; set; } = CascadeType.NoCascade;

		[Option("cascadeArchive", "caarc", false, "The behavior to apply to child records when the parent record is archived\n(not available via UI)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp = true)]
		public CascadeType CascadeArchive { get; set; } = CascadeType.NoCascade;

		[Option("cascadeShare", "cas", false, "The behavior to apply to child records when the parent record is shared\n(values: Cascade, Active, UserOwned, NoCascade)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp = true)]
		public CascadeType CascadeShare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeUnshare", "cau", false, "The behavior to apply to child records when the parent record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp = true)]
		public CascadeType CascadeUnshare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeDelete", "cad", false, "The behavior to apply when the parent record is deleted\n(values: Restrict, RemoveLink)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType CascadeDelete { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge", "cam", false, "The behavior to apply to child records when the parent record is merged to another one\n(not available via UI)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp = true)]
		public CascadeType CascadeMerge { get; set; } = CascadeType.NoCascade;

		[Option("cascadeReparent", "car", false, "The behavior to apply to child records when the parent record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)", DefaultValue = CascadeType.NoCascade, SuppressValuesHelp =true)]
		public CascadeType CascadeReparent { get; set; } = CascadeType.UserOwned;






		[Option("menuBehavior", "m", false, "Indicates", DefaultValue = AssociatedMenuBehavior.DoNotDisplay)]
		public AssociatedMenuBehavior MenuBehavior { get; set; } = AssociatedMenuBehavior.DoNotDisplay;

		[Option("menuLabel", "ml", false, "Associated menu label. To be specified only if the menuBehavior arg is set to UseLabel", DefaultValue = null)]
		public string? MenuLabel { get; set; }

		[Option("menuGroup", "mg", false, "Associated menu group. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = AssociatedMenuGroup.Details)]
		public AssociatedMenuGroup MenuGroup { get; set; } = AssociatedMenuGroup.Details;

		[Option("menuOrder", "mo", false, "Associated menu order. To be specified only if the menuBehavior arg is set to UseLabel or UseCollectionName", DefaultValue = 10000)]
		public int MenuOrder { get; set; } = 10000;




		[Option("lookupDisplayName", "ldn", false, "The display name of the lookup attribute. If not specified, the display name of the parent table is taken as default.")]
		public string? LookupAttributeDisplayName { get; set; }

		[Option("lookupSchemaName", "lsn", false, "The schema name of the lookup attribute. If not specified, the")]
		public string? LookupAttributeSchemaName { get; set; }

		[Option("requiredLevel", "r", HelpText = "The required level of the lookup attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;





		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this attribute.")]
		public string? SolutionName { get; set; }

	}
}
