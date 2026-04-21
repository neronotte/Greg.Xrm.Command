using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandLineArguments : List<string>, ICommandLineArguments
	{
		public CommandLineArguments(IEnumerable<string> args) : base(args)
		{
		}
	}
}