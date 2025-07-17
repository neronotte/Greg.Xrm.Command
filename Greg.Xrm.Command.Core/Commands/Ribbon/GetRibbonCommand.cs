namespace Greg.Xrm.Command.Commands.Ribbon
{
	[Command("ribbon", "get", HelpText = "Returns the full definition of a specific (application or table) ribbon (command bar).")]
	public class GetRibbonCommand
	{
		[Option("table", "t", HelpText = "The logical name of the table to get the ribbon for. If not specified, application ribbons are returned")]
		public string EntityName { get; set; } = string.Empty;


		[Option("output", "o", HelpText = "When specified, saves the ribbon definition in a local file. Should contain the name (absolute or relative to the current path) of the file that will contain the ribbon definition.")]
		public string FileName { get; set; } = string.Empty;

		[Option("autorun", "r", HelpText = "When specified, automatically opens the file containing the ribbon definition after export.", DefaultValue = false)]
		public bool AutoRun { get; set; } = false;
	}
}
