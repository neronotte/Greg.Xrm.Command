using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[Command("customapi", "bind",
		HelpText = "Binds a Dataverse Custom API to a plugin by setting its PluginTypeId lookup.")]
	public class BindCustomApiCommand : ICanProvideUsageExample
	{
		[Option("api", "a", Order = 1,
			HelpText = "Unique name of the Custom API (e.g. nn_GregSum).")]
		[Required]
		public string? ApiUniqueName { get; set; }

		[Option("plugin", "p", Order = 2,
			HelpText = "Full type name of the plugin as registered in Dataverse (e.g. MyNamespace.GregSumPlugin).")]
		[Required]
		public string? PluginTypeName { get; set; }

		[Option("assembly", Order = 3,
			HelpText = "Assembly name, required only when the plugin type name is ambiguous across assemblies.")]
		public string? AssemblyName { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Bind a Custom API to a plugin type (the plugin must already be registered in Dataverse):");
			writer.WriteCodeBlock("pacx customapi bind -a nn_GregSum -p MyNamespace.GregSumPlugin", "Powershell");

			writer.WriteParagraph("If the same plugin type name exists in multiple assemblies, disambiguate with --assembly:");
			writer.WriteCodeBlock("pacx customapi bind -a nn_GregSum -p MyNamespace.GregSumPlugin --assembly MyAssembly", "Powershell");

			writer.WriteParagraph("Use `pacx plugin list` to find the exact registered type name before binding.");
		}
	}
}
