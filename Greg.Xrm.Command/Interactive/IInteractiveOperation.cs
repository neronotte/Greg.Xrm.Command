using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Interactive
{
	interface IInteractiveOperation
	{
		string GetPromptText();

		CommandDefinition? GetCommand();

		IEnumerable<VerbNode> GetChildren();
	}
}
