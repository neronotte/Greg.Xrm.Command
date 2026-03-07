namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "boolean", HelpText = "Creates a boolean column.")]
	[Alias("column", "add", "bool")]
	[Alias("column", "add", "yesno")]
	public class CreateBooleanCommand : BaseCreateCommand
	{

		[Option("trueLabel", "tl", Order = 10, HelpText = "For Boolean type columns that represents the Label to be associated to the \"True\" value.", DefaultValue = "True")]
		public string? TrueLabel { get; set; } = "True";

		[Option("falseLabel", "fl", Order = 11, HelpText = "For  Boolean type columns that represents the Label to be associated to the \"False\" value.", DefaultValue = "False")]
		public string? FalseLabel { get; set; } = "False";
	}
}
