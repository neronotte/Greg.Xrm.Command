namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	[Command("column", "conventions", "set", HelpText = "Allows to customize default naming conventions used when creating columns.")]
	[Alias("column", "conventions", "update")]
	[Alias("column", "set-conventions")]
	[Alias("column", "setConventions")]
	public class SetCommand
	{
		[Option("simple-option-set-suffix", "soss", HelpText = "Suffix to be used for simple option set columns.")]
		public string? SimpleOptionSetSuffix { get; set; }

		[Option("multiselect-option-set-suffix", "msoss", HelpText = "Suffix to be used for multi select option set columns.")]
		public string? MultiselectOptionSetSuffix { get; set; }

		[Option("casing", "c", HelpText = "Casing style to be used for tables and columns schema names.")]
		public CasingStyle? Casing { get; set; }
	}
}
