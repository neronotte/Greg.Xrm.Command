using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "seed", HelpText = "Sets the seed for a given column. Is a proxy for [SetAutoNumberSeed Action](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/setautonumberseed?view=dataverse-latest)")]
	[Alias("column", "set-seed")]
	[Alias("column", "setSeed")]
	public class SetSeedCommand
	{
		[Option("table", "t", Order = 1, HelpText = "The logical name of the table.")]
		[Required]
		public string TableName { get; set; } = string.Empty;

		[Option("column", "c", Order = 2, HelpText = "The logical name of the column.")]
		[Required]
		public string ColumnName { get; set; } = string.Empty;

		[Option("seed", "s", Order = 3, HelpText = "The seed value to set.")]
		[Required]
		public int Seed { get; set; }
	}
}
