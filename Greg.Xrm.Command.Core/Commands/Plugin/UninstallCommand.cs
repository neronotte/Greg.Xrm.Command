using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
    [Command("plugin", "uninstall", HelpText = "Uninstalls a PACX plugin.")]
	[Alias("plugin", "remove")]
	[Alias("plugin", "delete")]
	[Alias("uninstall", "plugin")]
	[Alias("delete", "plugin")]
	[Alias("remove", "plugin")]
	public class UninstallCommand
	{
		[Option("name", "n", HelpText = "The unique name of the NuGet package containing the plugin to uninstall.")]
		[Required]
		public string Name { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}
