namespace Greg.Xrm.Command.Commands.Projects
{
	[Command("project", "resume", HelpText = "When called, enables the current project. All the subsequent commands will use the project's auth profile and solution.")]
	public class ResumeProjectCommand
	{
	}
}
