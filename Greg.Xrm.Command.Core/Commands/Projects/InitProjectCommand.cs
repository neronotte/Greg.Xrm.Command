
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Project;

namespace Greg.Xrm.Command.Commands.Projects
{
	[Command("project", "init", HelpText = "Initializes a new PACX project")]
    public class InitProjectCommand : ICanProvideUsageExample
	{
		[Option("conn", "c", HelpText = "The name of the authentication profile that allows to connect to the environment associated to the current command. It overrides the default auth profile set via `pacx auth select`. If not provided, the current default auth profile is used.")]
		public string? AuthenticationProfileName { get; set; }


		[Option("solution", "s", HelpText = "The default solution to use to store the customizations for the current project. It overrides any solution set via `pacx solution setDefault`. If not provided, the default solution for the selected auth profile is used.")]
		public string? SolutionName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteTitle3("The problem")
				.WriteLine("By default PACX commands execute in a global context.")
				.WriteLine("The authentication profile to be used to connect to a Dataverse instance, or the solution to work with, are defined in a global PACX settings file.")
				.WriteLine()
				.WriteLine("It's the same approach used by PAC CLI, and it's really helpful when scripting, because you can autenticate once, define once the solution to work with, and all the subsequent commands will inherit that configuration.")
				.WriteLine()
				.WriteLine("Sometimes, however, it may became cumbersome when working on multiple projects, or when working with a segmented solution approach on a given project, because it requires you to switch configuration often.")
				.WriteLine();

			writer.WriteTitle3("The solution")
				.WriteLine("The `project init` command allows you to create a **local configuration file** in the **current directory**, that will **override** the global settings **for all the subsequent commands that will run from the current folder, or one of its children**.")
				.WriteLine();

			writer.WriteParagraph("You can simply type: ");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx project init");
			writer.WriteLine(" -- or --");
			writer.WriteLine("pacx project init --conn <auth profile name> --solution <default solution unique name>");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine($"This will create a `{PacxProject.FileName}` file in the current directory, that will store the authentication profile and default solution name to be used by commands that will run in the context of the current project. Both arguments are optional. If not specified, the current global values will be used.")
				.WriteLine($"A folder containing a `{PacxProject.FileName}` file is called a PACX project folder.")
				.WriteLine();

			writer.WriteParagraph("To show the details of the current project, you can use the `info` command:")
				.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx project info")
				.WriteCodeBlockEnd()
				.WriteLine();

			writer.WriteTitle3("Suspend / Resume default settings override")
				.WriteParagraph($"Running a command inside a PACX project folder overrides the default auth. profile and default solution. If you need to temporary suspend this override, without deleting the `{PacxProject.FileName}` file, you can do it via:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx project suspend");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteParagraph("Once you suspended the project, all subsequent commands will run against the global, default, auth profile and solution.");

			writer.WriteParagraph($"To resume the override, you can use the `resume` command:");
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("pacx project resume");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();
		}
	}
}
