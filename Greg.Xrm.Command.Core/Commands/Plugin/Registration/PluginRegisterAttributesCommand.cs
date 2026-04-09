using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "register-attributes", HelpText = "Scan DLLs for [CrmPluginStep] attributes and auto-register plugin steps in Dataverse.")]
	public class PluginRegisterAttributesCommand
	{
		[Option("dll", "d", Order = 1, Required = true, HelpText = "Path to the plugin DLL file or directory containing DLLs.")]
		public string Path { get; set; } = "";

		[Option("solution", "s", Order = 2, HelpText = "Solution unique name to register plugins into. Defaults to active solution.")]
		public string? SolutionUniqueName { get; set; }

		[Option("publisher", "p", Order = 3, HelpText = "Publisher unique name. Defaults to 'devkit'.")]
		public string PublisherUniqueName { get; set; } = "devkit";

		[Option("publisher-name", Order = 4, HelpText = "Publisher friendly name. Defaults to 'Development Toolkit'.")]
		public string PublisherName { get; set; } = "Development Toolkit";

		[Option("dry-run", Order = 5, HelpText = "Scan and show what would be registered without actually registering.")]
		public bool DryRun { get; set; }

		[Option("format", "f", Order = 6, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("isolation", Order = 7, DefaultValue = "None", HelpText = "Plugin isolation mode: None, Sandbox. Default is None.")]
		public string IsolationMode { get; set; } = "None";
	}
}
