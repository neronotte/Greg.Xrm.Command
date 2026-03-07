
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "poly", "add", HelpText = "Adds a new parent to an existing many-to-one **polymorphic** relationship between Dataverse tables")]
	[Alias("rel", "poli", "add")]
	public class AddPolyCommand
	{

		[Option("child", "c", Order = 1, HelpText ="The child table (N side of the relationship)")]
		[Required]
		public string ChildTable { get; set; } = string.Empty;


		[Required]
		[Option("lookup", "l", Order = 2, HelpText = "The lookup column that represent the relationship to update.")]
		public string LookupColumnName { get; set; } = string.Empty;


		[Option("parent", "p", Order = 3, HelpText = "The parent table to add to the relationship")]
		[Required]
		public string ParentTable { get; set; } = string.Empty;



		[Option("relNameSuffix", "suff", Order = 4, HelpText = "The suffix to append to the relationship name. If not provided, will be set equal to the display name of the lookup attribute (only letters, numbers, or underscores, lowercase).")]
		public string? RelationshipNameSuffix { get; set; }



		[Option("cascadeAssign", "caass", Order = 5, HelpText = "The behavior to apply to child records when the parent record is assigned to another owner\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeAssign { get; set; } = CascadeType.NoCascade;

		[Option("cascadeArchive", "caarc", Order = 6, HelpText = "The behavior to apply to child records when the parent record is archived\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeArchive { get; set; } = CascadeType.NoCascade;

		[Option("cascadeShare", "cas", Order = 7, HelpText = "The behavior to apply to child records when the parent record is shared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeShare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeUnshare", "cau", Order = 8, HelpText ="The behavior to apply to child records when the parent record is unshared\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeUnshare { get; set; } = CascadeType.NoCascade;

		[Option("cascadeDelete", "cad", Order = 9, HelpText = "The behavior to apply when the parent record is deleted\n(values: Restrict, RemoveLink)\n(default: Restrict)", DefaultValue = CascadeType.Restrict, SuppressValuesHelp = true)]
		public CascadeType? CascadeDelete { get; set; } = CascadeType.Restrict;

		[Option("cascadeMerge", "cam", Order = 10, HelpText = "The behavior to apply to child records when the parent record is merged to another one\n(not available via UI)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeMerge { get; set; } = CascadeType.NoCascade;

		[Option("cascadeReparent", "car", Order = 11, HelpText = "The behavior to apply to child records when the parent record is reparented\n(values: Cascade, Active, UserOwned, NoCascade)\n(default: NoCascade)", SuppressValuesHelp = true)]
		public CascadeType? CascadeReparent { get; set; } = CascadeType.NoCascade;






		[Option("solution", "s", Order = 100, HelpText = "The name of the unmanaged solution that contains the table (used to get the publisher prefix). If not provided, the default table for the environment will be used.")]
		public string? SolutionName { get; set; }
	}
}
