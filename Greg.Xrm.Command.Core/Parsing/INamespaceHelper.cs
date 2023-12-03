namespace Greg.Xrm.Command.Parsing
{
	public interface INamespaceHelper
	{
		public string[] Verbs { get; }

		string GetHelp();
	}
}
