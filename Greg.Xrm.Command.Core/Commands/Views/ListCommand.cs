using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "list", HelpText = "List all the views available for a given Dataverse table")]
	public class ListCommand
	{
		[Option("table", "t", HelpText = "The table name for which to list the views")]
		[Required]
		public string TableName { get; set; } = string.Empty;


		[Option("type", "q", HelpText = "The type of query to list.", DefaultValue = QueryType.SavedQuery)]
		public QueryType QueryType { get; set; } = QueryType.SavedQuery;
	}


	public enum QueryType
	{
		SavedQuery,
		UserQuery,
		Both
	}
}
