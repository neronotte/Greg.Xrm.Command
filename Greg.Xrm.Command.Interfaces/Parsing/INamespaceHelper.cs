using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Parsing
{
	public interface INamespaceHelper
	{
		public string[] Verbs { get; }

		string GetHelp();
		void WriteHelp(MarkdownWriter writer);
	}
}
