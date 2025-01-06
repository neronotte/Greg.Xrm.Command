namespace Greg.Xrm.Command.Commands.Projects
{
	[Command("project", "info", HelpText = "If the current folder is under a PACX project folder, shows the details of the current project")]
	[Alias("project", "get")]
	public class InfoProjectCommand
	{
	}
}
