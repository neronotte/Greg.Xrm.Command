using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "step-scan", HelpText = "Validate plugin step definitions in compiled DLLs without deploying to Dataverse.")]
	public class PluginStepScanCommand
	{
		[Option("dll", "d", Order = 1, Required = true, HelpText = "Path to the plugin DLL file or directory containing DLLs.")]
		public string Path { get; set; } = "";

		[Option("format", "f", Order = 2, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";

		[Option("strict", Order = 3, HelpText = "Fail if any validation warnings are found.")]
		public bool Strict { get; set; }
	}
}
