using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "init", HelpText = "Set-up the folder that will host the Dataverse WebResources")]
	[Alias("wr", "init")]
	public class InitCommand : ICanProvideUsageExample
	{
		[Option("folder", "f", HelpText = "The folder where the webresources will be stored. If not provided, the current folder will be used.")]
		public string? Folder { get; set; }


		[Option("remote", "r", HelpText = "Indicates that the folder will be set-up synchronizing the webresources from the Dataverse environment")]
		public bool FromSolution { get; set; } = false;


		[Option("solution", "s", HelpText = "In case you want to init the folder from the contents of a solution that is not the default one for the current environment, specifies the name of the solution to take as source")]
		public string? SolutionName { get; set; }


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			// you can initialize the folder where the webresources will be stored
			// manually, or by synchronizing with webresources in the current solution

			writer.WriteParagraph("This command can be used to set-up the folder that will contain the WebResources used by your solution.");

			writer.WriteParagraph("The command will create a **.wr.pacx** file in the specified folder. That file will be used as placeholder to identify the root folder for the WebResources. It supports 2 main ways to initialize the folder:");

			writer.WriteLine("- From scratch, assuming no WebResource is already present in your solution");
			writer.WriteLine("- By synchronizing the folder with the WebResources in a given solution (using the --remote argument)");

			writer.WriteParagraph("If starting from scratch, the command will create the following folder structure:");

			writer.WriteLine("- **<publisher prefix>**: the folder that will contain the WebResources organized by publisher prefix (e.g. new, fabrikam, etc)");
			writer.WriteLine("  - **images**: the folder that will contain the images (png, svg, etc) used by your solution");
			writer.WriteLine("  - **scripts**: the folder that will contain the JavaScript files for Forms or Ribbons");
			writer.WriteLine("  - **pages**: the folder that will contain the custom HTML webresources");

			writer.WriteParagraph("If starting from a remote solution, it will create a folder structure reflecting the one in the solution.");
			writer.WriteParagraph("In both cases, a remote solution must be present and set as default solution for the environment (or provided using the --solution argument), because it is needed to determine the **publisher prefix** to set as root folder.");

			writer.WriteTitle3("Examples");

			writer.WriteParagraph("Initialize the current folder from scratch, taking the publisher from the default solution set on the current environment:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init");
			writer.WriteLine("xrm ws init");
			writer.WriteCodeBlockEnd();


			writer.WriteParagraph("Initialize the .\\webresources subfolder from scratch, taking the publisher from the default solution set on the current environment:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init --folder .\\webresources");
			writer.WriteLine("xrm ws init -f .\\webresources");
			writer.WriteCodeBlockEnd();


			writer.WriteParagraph("Initialize the current folder from the remote solution set as default for the current environment:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init --remote");
			writer.WriteLine("xrm ws init -r");
			writer.WriteCodeBlockEnd();


			writer.WriteParagraph("Initialize the current folder from the specified remote solution:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init --remote --solution my_solution");
			writer.WriteLine("xrm ws init -r -s my_solution");
			writer.WriteCodeBlockEnd();


			writer.WriteParagraph("Initialize the .\\webresources folder from the remote solution set as default for the current environment:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init --remote --folder .\\webresources");
			writer.WriteLine("xrm ws init -r -f .\\webresources");
			writer.WriteCodeBlockEnd();


			writer.WriteParagraph("Initialize the .\\webresources folder from the specified remote solution:");

			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("xrm webresources init --remote --solution my_solution --folder .\\webresources");
			writer.WriteLine("xrm ws init -r -s my_solution -f .\\webresources");
			writer.WriteCodeBlockEnd();

		}
	}
}
