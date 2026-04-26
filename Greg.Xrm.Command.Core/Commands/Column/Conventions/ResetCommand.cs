namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	[Command("column", "conventions", "reset", HelpText = "Restores the default naming conventions used when creating columns.")]
	[Alias("column", "reset-conventions")]
	[Alias("column", "resetConventions")]
	public class ResetCommand
	{
	}
}
