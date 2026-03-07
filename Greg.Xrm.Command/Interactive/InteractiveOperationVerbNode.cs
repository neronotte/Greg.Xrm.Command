using Greg.Xrm.Command.Parsing;
using Spectre.Console;

namespace Greg.Xrm.Command.Interactive
{
	class InteractiveOperationVerbNode(VerbNode node, int maxLength) : IInteractiveOperation
	{
		public CommandDefinition? GetCommand()
		{
			return node.Command;
		}

		public string GetPromptText()
		{
			return $"[{GetColor(node)}]{node.Verb.PadRight(maxLength)}[/][{DefaultColors.Text}] - {Normalize(node)}[/]";
		}



		static string GetColor(VerbNode node)
		{
			return node.Command is null ? DefaultColors.Namespace : DefaultColors.Command;
		}

		static string? Normalize(VerbNode node)
		{
			var text = node.Command is null ? node.Help : node.Command.HelpText;

			text = text?
				.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrEmpty(line))
				.FirstOrDefault();
			//.Aggregate((a, b) => $"{a} {b}");

			return text != null ? Markup.Escape(text) : null;
		}

		public IEnumerable<VerbNode> GetChildren()
		{
			return node.Children ?? [];
		}
	}
}
