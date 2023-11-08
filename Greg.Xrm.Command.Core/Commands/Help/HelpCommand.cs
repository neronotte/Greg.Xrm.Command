using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Help
{
	public class HelpCommand
	{
        public HelpCommand(CommandDefinition commandDefinition)
        {
			this.CommandDefinition = commandDefinition;
		}

		public HelpCommand(List<CommandDefinition> commandDefinitionList)
		{
			this.CommandList = commandDefinitionList;
		}

		public CommandDefinition? CommandDefinition { get; }
		public List<CommandDefinition> CommandList { get; } = new List<CommandDefinition>();
	}
}
