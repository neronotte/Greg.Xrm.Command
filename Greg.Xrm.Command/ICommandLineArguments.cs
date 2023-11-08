namespace Greg.Xrm.Command
{
	public interface ICommandLineArguments : IReadOnlyList<string>
	{
		void Remove(string arg);
	}
}