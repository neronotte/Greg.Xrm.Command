using System.Reflection;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Parsing.Attributes;
using Spectre.Console;

namespace Greg.Xrm.Command.Interactive
{
	class VerbTreeRecourser
	{
		public static CommandDefinition? Recourse(IAnsiConsole console, IEnumerable<VerbNode> tree, IEnumerable<VerbNode> rootTree)
		{
			var parents = BuildParentStack(tree, rootTree);

			IInteractiveOperation result;
			do
			{
				var verbsToShow = tree.Where(x => !x.IsHidden)
					.Where(x => x.Command is null || x.Command.CommandType.GetCustomAttribute<HideInInteractiveExperienceAttribute>() is null)
					.ToArray();

				var maxLength = verbsToShow.Max(node => node.Verb.Length);


				var operations = new List<IInteractiveOperation>();
				operations.AddRange(verbsToShow.Select(node => new InteractiveOperationVerbNode(node, maxLength)));

				if (tree != rootTree)
					operations.Add(InteractiveOperationBack.Instance);
				operations.Add(InteractiveOperationQuit.Instance);

				var prompt = new SelectionPrompt<IInteractiveOperation>()
					.Title($"Select [{DefaultColors.Namespace}]namespace[/] or [{DefaultColors.Command}]command[/] (or CTRL+C to exit):")
					.WrapAround()
					.EnableSearch()
					.SearchPlaceholderText("Type to search...")
					.HighlightStyle(new Style(Color.Black, Color.SandyBrown, Decoration.None))
					.UseConverter(node => node.GetPromptText())
					.PageSize(15)
					.AddChoices(operations);

				result = console.Prompt(prompt);

				if (result == InteractiveOperationBack.Instance)
				{
					if (parents.Count == 0)
						throw new InvalidOperationException("Cannot go back from the root tree.");

					tree = parents.Pop();
					continue;
				}
				if (result == InteractiveOperationQuit.Instance)
				{
					return null;
				}

				parents.Push(tree);
				tree = result.GetChildren();

			}
			while (result.GetCommand() is null);

			return result.GetCommand();
		}

		private static Stack<IEnumerable<VerbNode>> BuildParentStack(IEnumerable<VerbNode> tree, IEnumerable<VerbNode> rootTree)
		{
			if (tree == rootTree)
				return new Stack<IEnumerable<VerbNode>>();

			var parents = new Stack<IEnumerable<VerbNode>>();
			foreach (var child in rootTree)
			{
				if (child.Children == tree)
				{
					parents.Push(rootTree);
					return parents;
				}
				var result = BuildParentStack(tree, child.Children);
				if (result.Count > 0)
				{
					result.Push(rootTree);
					return result;
				}

			}
			return parents;
		}
	}
}
