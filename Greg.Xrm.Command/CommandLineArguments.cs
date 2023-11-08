namespace Greg.Xrm.Command
{
	public class CommandLineArguments : List<string>, ICommandLineArguments
	{
		public CommandLineArguments(string[] args) : base(args)
		{
		}

		void ICommandLineArguments.Remove(string arg)
		{
			this.Remove(arg);
		}
	}
}