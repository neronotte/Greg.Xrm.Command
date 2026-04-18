using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "getDependencies", HelpText = "Retrieves the list of solution components that depend from a given column")]
	[Alias("column", "getdeps")]
	[Alias("column", "get-dependencies")]
	public class GetDependenciesCommand
	{
		[Option("table", "t", Order = 1, HelpText = "The name of the table containing the column to retrieve the dependencies for")]
		[Required]
		public string? TableName { get; set; }

		[Option("column", "c", Order = 2, HelpText = "The name of the column to retrieve the dependencies for")]
		[Required]
		public string? ColumnName { get; set; }


		[Option("forDelete", "d", Order = 3, HelpText = "Specifies whether to retrieve the dependencies for delete or not", DefaultValue = true)]
		public bool ForDelete { get; set; } = true;
	}
}
