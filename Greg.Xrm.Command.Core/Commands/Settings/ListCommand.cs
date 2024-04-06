
namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "list", HelpText = "List settings defined for the current environment")]
	public class ListCommand
	{
		[Option("origin", "o", "Indicates if the list of settings to retrieve is the whole list of settings, or just the settings in the specified solution.", DefaultValue = Origin.Solution)]
		public Origin Origin { get; set; } = Origin.Solution;

		[Option("filter", "f", "Indicates if the list of settings to retrieve should include all settings, or only visible settings.", DefaultValue = Which.Visible)]
		public Which Filter { get; set; } = Which.Visible;

		[Option("solution", "s", HelpText = "The solution to get the settings from. If not specified, the default solution is considered.")]
		public string? SolutionName { get; set; }
	}


	public enum Origin
	{
		Solution,
		All
	}

	public enum Which
	{
		Visible,
		All
	}
}
