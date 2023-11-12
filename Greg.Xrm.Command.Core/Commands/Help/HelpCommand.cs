using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Help
{
	public class HelpCommand
	{
        public HelpCommand(CommandDefinition commandDefinition)
        {
			this.CommandDefinition = commandDefinition;
			this.ExportHelp = false;
			this.ExportHelpPath = string.Empty;
		}

		public HelpCommand(List<CommandDefinition> commandDefinitionList, IReadOnlyDictionary<string, string> options)
		{
			this.CommandList = commandDefinitionList;
			this.ExportHelp = options.ContainsKey("--export");
			this.ExportHelpPath = this.ExportHelp ? options["--export"] : string.Empty;
		}

		public CommandDefinition? CommandDefinition { get; }

		public bool ExportHelp { get; }

		public string ExportHelpPath { get; }

		public List<CommandDefinition> CommandList { get; } = new List<CommandDefinition>();
	}
}
