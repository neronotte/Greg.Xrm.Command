using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Help
{
	public class HelpCommand
	{
		public HelpCommand(VerbNode lastMatchingVerb)
		{
			this.ExportHelp = false;
			this.ExportHelpPath = string.Empty;
			this.LastMatchingVerb = lastMatchingVerb;
		}


        public HelpCommand(CommandDefinition commandDefinition)
        {
			this.CommandDefinition = commandDefinition;
			this.ExportHelp = false;
			this.ExportHelpPath = string.Empty;
			this.LastMatchingVerb = null;
		}

		public HelpCommand(List<CommandDefinition> commandDefinitionList, List<VerbNode> commandTree, IReadOnlyDictionary<string, string> options)
		{
			this.CommandList = commandDefinitionList;
			this.CommandTree = commandTree;
			this.ExportHelp = options.ContainsKey("--export");
			this.ExportHelpPath = this.ExportHelp ? options["--export"] : string.Empty;
			this.LastMatchingVerb = null;
		}





		public CommandDefinition? CommandDefinition { get; }

		public bool ExportHelp { get; }

		public string ExportHelpPath { get; }

		public List<CommandDefinition> CommandList { get; } = new();

		public List<VerbNode> CommandTree { get; } = new();

		public VerbNode? LastMatchingVerb { get; }
	}
}
