namespace Greg.Xrm.Command.Commands.Projects
{
	[Command("project", "suspend", HelpText = "When called, disables the current project. All the subsequent commands will fall back to the default auth profile and solution.")]
	public class SuspendProjectCommand
	{
	}
}
