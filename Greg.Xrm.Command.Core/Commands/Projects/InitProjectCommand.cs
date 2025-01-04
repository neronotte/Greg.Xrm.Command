
namespace Greg.Xrm.Command.Commands.Projects
{
	[Command("project", "init")]
    public class InitProjectCommand
	{
		[Option("conn", "c", HelpText = "The name of the authentication profile that allows to connect to the environment associated to the current command. It overrides the default auth profile set via `pacx auth select`. If not provided, the current default auth profile is used.")]
		public string? AuthenticationProfileName { get; set; }


		[Option("solution", "s", HelpText = "The default solution to use to store the customizations for the current project. It overrides any solution set via `pacx solution setDefault`. If not provided, the default solution for the selected auth profile is used.")]
		public string? SolutionName { get; set; }	
	}
}
