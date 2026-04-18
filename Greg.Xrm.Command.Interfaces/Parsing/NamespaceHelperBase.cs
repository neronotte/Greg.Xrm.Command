using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Parsing
{
	public abstract class NamespaceHelperBase : INamespaceHelper
	{
		private readonly string help;

		protected NamespaceHelperBase(string help, params string[] verbs)
		{
			this.Hidden = false;
			this.help = help;
			this.Verbs = verbs;
		}

		protected NamespaceHelperBase(bool hidden, string help, params string[] verbs)
		{
			this.Hidden = hidden;
			this.help = help;
			this.Verbs = verbs;
		}

		public string[] Verbs { get; }
		public bool Hidden { get; protected set; }

		public string GetHelp()
		{
			return this.help;
		}

		public virtual void WriteHelp(MarkdownWriter writer)
		{
			writer.WriteParagraph(this.GetHelp());
		}
	}
}
