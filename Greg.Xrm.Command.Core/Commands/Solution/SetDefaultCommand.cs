namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "setdefault", HelpText = "Sets the default solution for the current environment")]
	public class SetDefaultCommand
	{
		[Option("name", "un", IsRequired = true, HelpText = "The unique name of the solution to set as default")]
        public string? SolutionUniqueName { get; set; }
    }
}
