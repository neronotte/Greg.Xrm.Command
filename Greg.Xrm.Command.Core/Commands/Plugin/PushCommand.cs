using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "push", HelpText = "Push plugins packages or plugin assemblies into a Dataverse instance.")]
	public class PushCommand : ICanProvideUsageExample
	{
		[Option("path", "p", HelpText = "Path to the plugin package (*.nupkg) or plugin assembly (*.dll) to push.")]
		[Required]
		public string Path { get; set; } = string.Empty;


		[Option("solution", "s", HelpText = "The name of the solution where package must be added (in case of creation). If not provided, the default solution will be used.")]
		public string? SolutionName { get; set; }



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("This command can be used to register or update plugin packages or plugin assemblies into Dataverse.");
			writer.WriteLine("You must use the `--path` option to specify the file (`*.nupkg` or `*.dll`) to upload.");
			writer.WriteLine();
			writer.WriteLine("> **HINT**: prefer the usage of plugin packages (*.nupkg) if available. If not available, fallback to plugin assemblies (*.dll).");
			writer.WriteLine();
			writer.WriteLine("> **PLEASE NOTE**: If you register a plugin assembly (*.dll), CodeActivities won't be registered. CodeActivities are used in legacy workflows and should not be used anymore. If you need to use them, we suggest to use **PluginRegistrationTool** (`pac tool prt`) to register the assembly.");
			writer.WriteLine();
		}
	}
}
