using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "clone", HelpText = "Creates a copy of a given view.")]
	public class CloneCommand
	{
		[Option("old", "o", Order = 1, HelpText = "The display name of the view to clone.")]
		[Required]
		public string OldName { get; set; } = string.Empty;

		[Option("new", "n", Order = 2, HelpText = "The new name of the view. If not provided, a suffix will be set by default.")]
		public string NewName { get; set; } = string.Empty;


		[Option("table", "t", Order = 3, HelpText = "The name of the table that contains the view. Required only if the view name is not unique in the system.")]
		public string? TableName { get; set; }


		[Option("type", "q", Order = 4, HelpText = "The type of query.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;


		[Option("clean", "c", Order = 5, HelpText = "Indicates that during the clone operation, all the filters applied on the previous view must be removed from the new view.")]
		public bool Clean { get; set; } = false;


		[Option("solution", "s", Order = 6, HelpText = "Specifies the name of the solution that will contain the view after the creation. If not specified, the default solution for the current environment is used.")]
		public string? SolutionName { get; set; }
	}
}
