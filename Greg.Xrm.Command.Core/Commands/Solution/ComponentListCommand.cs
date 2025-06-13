namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "list", HelpText = "Returns the list of components in a given solution.")]
	public class ComponentListCommand
	{
		[Option("solution", "s", HelpText = "The name of the solution. If not provided, the default solution will be used.")]
		public string? SolutionName { get; set; }


		[Option("format", "f", HelpText = "Chooses how to generate the output.", DefaultValue = OutputFormat.Table)]
		public OutputFormat Format { get; set; } = OutputFormat.Table;


		public enum OutputFormat
		{
			Table,
			Json
		}
	}
}
