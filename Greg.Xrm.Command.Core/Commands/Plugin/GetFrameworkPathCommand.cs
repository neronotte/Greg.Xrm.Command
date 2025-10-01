namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "get-framework-path", HelpText = "Returns the path to the .NET Framework 4.6.2 reference assemblies used for plugin assembly validation.")]
	[Alias("plugin", "gfp")]
	public class GetFrameworkPathCommand
	{
	}
}
