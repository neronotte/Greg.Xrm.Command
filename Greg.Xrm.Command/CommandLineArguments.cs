using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command
{
	public class CommandLineArguments : List<string>, ICommandLineArguments
	{
		public CommandLineArguments(string[] args) : base(args)
		{
		}
	}
}