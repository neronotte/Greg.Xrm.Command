using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
    [Command("webresources", "push", HelpText = "Push web resources from a local folder to the target environment")]
	[Alias("wr", "push")]
	public class PushCommand : ICanProvideUsageExample
	{
		[Option("path", "p", HelpText = "The path to the folder containing the webresources to deploy, or to a specific webresource file to deploy. If not provided, the current folder will be used.")]
		public string? Folder { get; set; }

		[Option("no-publish", "np", HelpText = "Indicates that the webresources should be published after the deployment", DefaultValue =false)]
		public bool NoPublish { get; set; } = false;

		[Option("no-action", "nop", HelpText = "If specified, the command will not perform any action, but it will show what it would do.", DefaultValue = false)]
		public bool NoAction { get; set; } = false;

		[Option("solution", "s", HelpText = "The name of the solution that will contain the WebResources. If empty, the default solution for the current environment is used as default")]
		public string? SolutionName { get; set; }


		[Option("verbose", "v", HelpText = "If specified, the command will output more details about the operations performed")]
		public bool Verbose { get; set; } = false;


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command is basically a simple Command Line based replacement for XrmToolbox WebResource Manager");

			writer.WriteTitle3("A bit of theory");

			writer.WriteParagraph("--- work in progress ---");


			writer.WriteTitle3("How to use it");

			writer.WriteParagraph("To deploy all the webresources that are in the **current folder** to the target environment, you can use the following command:");
			
			writer.WriteCodeBlock("pacx webresources push", "Powershell");

			writer.WriteParagraph("The command will:");
			writer.WriteList(
				"Connect to the target environment",
				"Compare the the list of files in the current folder with the list of webresources in the target environment",
				"Upload the webresources that are missing in the target environment",
				"Update the webresources that are different in the target environment",
				"Add the webresources to the solution (if not already added previously)",
				"Publish the webresources"
			);

			writer.WriteParagraph("If you want to deploy a specific webresource file, you can specify the path to the file:");

			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources push --path \"C:\\path\\to\\file.js\"")
				.WriteLine("pacx webresources push --path \"file.js\"")
				.WriteCodeBlockEnd();

			writer.WriteLine("If you used `pacx wr init` to initialize the webresource project, you can use the $ special token to refer to the nearest project root folder, _navigating up_ from the current folder.");
			writer.WriteLine("It will take only the files that are below `<project root folder>/<current solution publisher prefix>_`.");
			writer.WriteLine();
			writer.WriteLine("For example, imagine to have the following project structure:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("Webresources/");
			writer.WriteLine("    new_/");
			writer.WriteLine("        scripts/");
			writer.WriteLine("            account.js");
			writer.WriteLine("        pages/");
			writer.WriteLine("            home/");
			writer.WriteLine("                home.css");
			writer.WriteLine("                home.js");
			writer.WriteLine("                home.html");
			writer.WriteLine("    home_react_source/");
			writer.WriteLine("        src/");
			writer.WriteLine("            ...");
			writer.WriteLine("    .wr.pacx");
			writer.WriteCodeBlockEnd();
			
			writer.WriteParagraph("And you are placed under the `Webresources/home_react_source/src` folder, by typing:");

			writer.WriteCodeBlock("pacx webresources push -f $", "Powershell");

			writer.WriteParagraph("It will: ");
			writer.WriteList(
				"Navigate up from the current folder until it comes to the folder containing the `.wr.pacx` file",
				"From there, navigates under the `new_` folder (assuming that `new` is the customization prefix of the current solution publisher)",
				"Recourse all the folders under `new_`, take all the files to deploy them to the target environment"
			);

			writer.WriteParagraph("You can also use the $ to target a specific subfolder:");

			writer.WriteCodeBlock("pacx webresources push -f $\\scripts", "Powershell");


			writer.WriteParagraph("You can also use the * wildcard in the file name to match for files with a specific extension. For example, the following command will deploy all the files with the .js extension from the current folder:");

			writer.WriteCodeBlock("pacx webresources push -f *.js", "Powershell");

			writer.WriteParagraph("If you want to deploy the webresources without publishing them, you can use the following command:");

			writer.WriteCodeBlockStart("Powershell")
				.WriteLine("pacx webresources push --no-publish")
				.WriteLine("pacx webresources push -np")
				.WriteCodeBlockEnd();

			writer.WriteParagraph("> **PLEASE NOTE**: by default, all files with *.data.xml extensions are excluded from the upload, because it is assumed that those file may come from a `pac solution clone` command (or similar).");
		}
	}
}
