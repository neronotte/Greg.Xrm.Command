using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "getDependencies", HelpText = "Retrieves the list of solution components that depend from a given column")]
	[Alias("column", "getdeps")]
	[Alias("column", "get-dependencies")]
	public class GetDependenciesCommand
	{
		[Option("table", "t", HelpText = "The name of the table containing the column to retrieve the dependencies for")]
		[Required]
		public string? TableName { get; set; }

		[Option("column", "c", HelpText = "The name of the column to retrieve the dependencies for")]
		[Required]
		public string? ColumnName { get; set; }
	}
}
