
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "add", "poly", HelpText = "Adds a new parent to an existing many-to-one **polymorphic** relationship between Dataverse tables")]
	[Alias("rel", "add", "poli")]
	public class AddPolyCommand
	{

		[Option("child", "c", "The child table (N side of the relationship)")]
		[Required]
		public string ChildTable { get; set; } = string.Empty;


		[Required]
		[Option("lookup", "l", "The lookup column that represent the relationship to update.")]
		public string LookupColumnName { get; set; } = string.Empty;


		[Option("parent", "p", "The parent table to add to the relationship")]
		[Required]
		public string ParentTable { get; set; } = string.Empty;



		[Option("relNameSuffix", "suff", "The suffix to append to the relationship name. If not provided, will be set equal to the display name of the lookup attribute (only letters, numbers, or underscores, lowercase).")]
		public string? RelationshipNameSuffix { get; set; }



		[Option("cascadeAssign", "caass", "The behavior to apply to child records when the parent record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign { get; set; } = CascadeType.NoCascade;

		[Option("cascadeArchive", "caarc", "The behavior to apply to child records when the parent record is archived\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive { get; set; } = CascadeType.NoCascade;

		[Option("cascadeShare", "cas", "The behavior to apply to child records when the parent record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeUnshare", "cau", "The behavior to apply to child records when the parent record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeDelete", "cad", "The behavior to apply when the parent record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge", "cam", "The behavior to apply to child records when the parent record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge { get; set; } = CascadeType.NoCascade;

		[Option("cascadeReparent", "car", "The behavior to apply to child records when the parent record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeReparent { get; set; } = CascadeType.NoCascade;






		[Option("solution", "s", HelpText = "The name of the unmanaged solution that contains the table (used to get the publisher prefix). If not provided, the default table for the environment will be used.")]
		public string? SolutionName { get; set; }
	}
}
