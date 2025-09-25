using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Tool
{
    [Command("tool", "uninstall", HelpText = "Uninstalls a PACX plugin.")]
	[Alias("tool", "remove")]
	[Alias("tool", "delete")]
	[Alias("uninstall", "tool")]
	[Alias("delete", "tool")]
	[Alias("remove", "tool")]
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
