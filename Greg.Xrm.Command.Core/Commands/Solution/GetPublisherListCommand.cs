namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "getPublisherList", HelpText = "Lists the available publishers in current Dataverse environment. It displays unique name, friendly name and prefix.")]
	public class GetPublisherListCommand
	{
		[Option("verbose", "v", HelpText = "Add optionset prefix, created on, created by and description details.", DefaultValue = false)]
		public bool Verbose { get; set; } = false;
	}
}
