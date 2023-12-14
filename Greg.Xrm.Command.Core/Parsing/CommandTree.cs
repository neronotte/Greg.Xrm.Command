namespace Greg.Xrm.Command.Parsing
{
	public class CommandTree : List<VerbNode>
	{
		public VerbNode? FindNode(IReadOnlyList<string> verbs)
		{
			var node = this.Find(_ => string.Equals(_.Verb, verbs[0], StringComparison.OrdinalIgnoreCase));
			if (node == null) return null;

			for (int i = 1; i < verbs.Count; i++)
			{
				var child = node.Children.Find(_ => string.Equals(_.Verb, verbs[i], StringComparison.OrdinalIgnoreCase));
				if (child == null) return node;

				node = child;
			}

			return node;
		}
	}
}
