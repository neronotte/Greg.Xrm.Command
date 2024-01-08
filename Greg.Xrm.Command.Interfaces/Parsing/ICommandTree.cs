namespace Greg.Xrm.Command.Parsing
{
	public interface ICommandTree : IReadOnlyList<VerbNode>
	{
		VerbNode? FindNode(IReadOnlyList<string> verbs);
	}
}
