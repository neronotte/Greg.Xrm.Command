using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "poly", "remove", HelpText = "Removes a parent from an existing many-to-one **polymorphic** relationship between Dataverse tables")]
	[Alias("rel", "poli", "remove")]
	[Alias("rel", "poly", "rem")]
	[Alias("rel", "poli", "rem")]
	public class RemovePolyCommand
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
	}
}
