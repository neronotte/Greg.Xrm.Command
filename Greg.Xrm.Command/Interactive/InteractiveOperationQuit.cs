using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Interactive
{
	class InteractiveOperationQuit : IInteractiveOperation
	{
		public static readonly InteractiveOperationQuit Instance = new InteractiveOperationQuit();

		private InteractiveOperationQuit() { }

		public string GetPromptText()
		{
			return $"X  Quit";
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
