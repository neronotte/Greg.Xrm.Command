using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "rename", HelpText = "Changes the name of a given view")]
	public class RenameCommand
	{
		[Option("old", "o", Order = 1, HelpText = "The display name of the view to rename.")]
		[Required]
		public string OldName { get; set; } = string.Empty;


		[Option("new", "n", Order = 2, HelpText = "The new name of the view.")]
		[Required]
		public string NewName { get; set; } = string.Empty;


		[Option("table", "t", Order = 3, HelpText = "The name of the table that contains the view. Required only if the view name is not unique in the system.")]
		public string? TableName { get; set; }

		[Option("type", "q", Order = 4, HelpText = "The type of query.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;
	}




	public enum QueryType1
	{
		SavedQuery,
		UserQuery
	}
}
