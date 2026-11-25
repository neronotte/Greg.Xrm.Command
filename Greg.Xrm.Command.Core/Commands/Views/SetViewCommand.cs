using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	public abstract class SetViewCommand
	{
		[Option("name", "n", Order = 1, HelpText = "The display name of the view to update.")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("table", "t", Order = 10, HelpText = "The table containing the view. Required if the name is not unique.")]
		public string? TableName { get; set; }

		[Option("type", "q", Order = 11, HelpText = "SavedQuery updates a system view; UserQuery updates a personal view.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;

		[Option("publish", "p", Order = 20, HelpText = "Publish the view's table after updating. Defaults to false.", DefaultValue = false)]
		public bool Publish { get; set; }
	}
}
