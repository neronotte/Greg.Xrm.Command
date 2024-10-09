using Greg.Xrm.Command;

namespace SamplePlugin
{
	[Command("sample", HelpText = "Sample plugin that does a simple echo")]
	public class SampleCommand
	{
		[Option("echo", HelpText = "Echo message")]
		public string? Echo { get; set; }
	}
}
