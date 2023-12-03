namespace Greg.Xrm.Command.Parsing
{
	public abstract class NamespaceHelperBase : INamespaceHelper
	{
		private readonly string help;

		protected NamespaceHelperBase(string help, params string[] verbs)
        {
			this.help = help;
			this.Verbs = verbs;
		}

		public string[] Verbs { get; }

		public string GetHelp()
		{
			return this.help;
		}
	}
}
