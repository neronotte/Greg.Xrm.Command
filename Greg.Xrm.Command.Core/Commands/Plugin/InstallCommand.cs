using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
    [Command("plugin", "install", HelpText = "Installs or updates a PACX plugin.")]
    [Alias("plugin", "add")]
    [Alias("plugin", "get")]
    public class InstallCommand : ICanProvideUsageExample, IValidatableObject
    {
        [Option("name", "n", HelpText = "To install from NuGet. The unique name of the NuGet package containing the plugin to install.")]
        public string Name { get; set; } = string.Empty;

        [Option("version", "v", HelpText = "To install from NuGet. Allows to explicit select the version of the plugin to install.")]
        public string Version { get; set; } = string.Empty;

        [Option("source", "s", HelpText = "To install from other NuGet feed. Allows to explicit select the version of the plugin to install.")]
        public string Source { get; set; } = string.Empty;

        [Option("personalaccesstoken", "pat", HelpText = "Personal Access Token to authenticate to private NuGet feeds.")]
        public string PersonalAccessToken { get; set; } = string.Empty;

        [Option("file", "f", HelpText = "To install from a local file. The full path + file name of the nuget package containing the plugin to install.")]
        public string FileName { get; set; } = string.Empty;



        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(FileName))
            {
                yield return new ValidationResult("You must specify either the name of the plugin to install, or the full path to the .nupkg file.", [nameof(Name), nameof(FileName)]);
            }

            if (!string.IsNullOrWhiteSpace(FileName) && !string.IsNullOrWhiteSpace(Version))
            {
                yield return new ValidationResult("Version can be specified only when installing a plugin from NuGet.", [nameof(Version)]);
            }

            if (!string.IsNullOrWhiteSpace(Source) && string.IsNullOrWhiteSpace(PersonalAccessToken))
            {
                yield return new ValidationResult("You must specify the Personal Access Token to authenticate to the private NuGet feed.", [nameof(PersonalAccessToken)]);
            }
        }



        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("You can install a plugin from NuGet, or from a local .nupkg file.");
            writer.WriteParagraph("To install a plugin from NuGet, use the following command:");

            writer.WriteCodeBlockStart("Powershell");
            writer.WriteLine("pacx plugin install -n MyPlugin");
            writer.WriteLine("pacx plugin install -n MyPlugin -v 1.0.0");
            writer.WriteCodeBlockEnd();

            writer.WriteParagraph("To install a plugin from a local file, use the following command:");
            writer.WriteCodeBlock("pacx plugin install -f \"C:\\path\\MyPlugin.1.0.0.nupkg\"", "Powershell");

            writer.WriteParagraph("**!!! WARNING!!!**");
            writer.WriteParagraph("**Plugin content can be harmful**. Be sure to download and install only plugins whose source/author is trusted. PACX creators deny any liability that my be associated with an improper use of PACX plugins.");
        }
    }
}
