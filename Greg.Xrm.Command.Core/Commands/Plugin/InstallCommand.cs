using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "install", HelpText = "Installs or updates a PACX plugin.")]
    [Alias("plugin", "add")]
    [Alias("plugin", "get")]
    public class InstallCommand : ICanProvideUsageExample
	{
        [Option("name", "n", HelpText = "The unique name of the NuGet package containing the plugin to install.")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Option("version", "v", HelpText = "Allows to explicit select the version of the plugin to install.")]
        public string Version { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
            writer.WriteParagraph("**!!! WARNING!!!**");
            writer.WriteParagraph("**Plugin content can be harmful**. Be sure to download and install only plugins whose source/author is trusted. PACX creators deny any liability that my be associated with an improper use of PACX plugins.");
		}
	}
}
