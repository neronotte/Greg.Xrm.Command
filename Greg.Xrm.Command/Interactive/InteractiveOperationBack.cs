using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Interactive
{
	class InteractiveOperationBack : IInteractiveOperation
	{
		public static readonly InteractiveOperationBack Instance = new InteractiveOperationBack();

		private InteractiveOperationBack() { }

		public string GetPromptText()
		{
			return $"<- Back";
		}
		public CommandDefinition? GetCommand()
		{
			return null;
		}

		public IEnumerable<VerbNode> GetChildren()
		{
			return [];
		}
	}
}
