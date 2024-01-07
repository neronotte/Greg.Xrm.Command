namespace Greg.Xrm.Command.Parsing
{
	public interface ICommandLineArguments : IReadOnlyList<string>
	{
		bool Remove(string arg);
		void RemoveAt(int index);

		int IndexOf(string arg);
	}
}