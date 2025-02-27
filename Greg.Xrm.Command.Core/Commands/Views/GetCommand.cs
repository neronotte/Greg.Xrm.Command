using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "get", HelpText = "Retrieves the definition (fetchxml and layoutxml) of a given view")]
	public class GetCommand
	{
		[Option("name", "n", HelpText = "The display name of the view to retrieve.")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("table", "t", HelpText = "The name of the table that contains the view. Required only if the view name is not unique in the system.")]
		public string? TableName { get; set; }

		[Option("type", "q", HelpText = "The type of query.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;
	}
}
