using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Help
{
    public class HelpGeneratorGeneric
	{
		private readonly IOutput output;
		private readonly IReadOnlyList<CommandDefinition> commandList;
		private readonly IReadOnlyList<VerbNode> commandTree;

		public HelpGeneratorGeneric(IOutput output, IReadOnlyList<CommandDefinition> commandList, IReadOnlyList<VerbNode> commandTree)
        {
			this.output = output;
			this.commandList = commandList;
			this.commandTree = commandTree;
		}

		public void GenerateHelp2()
		{
			output.Write("Usage: ");
			output.Write(Assembly.GetEntryAssembly()?.GetName().Name, ConsoleColor.DarkCyan);
			output.Write(" ");

			foreach (var node in commandTree.Where(x => !x.IsHidden).OrderBy(x => x.Verb))
			{
				var color = node.Command is not null ? ConsoleColor.White : ConsoleColor.DarkCyan;

				output.Write("[", color).Write(node.Verb, color).Write("] ", color);
			}

			output.WriteLine().WriteLine();


			var padding = 28;
			foreach (var node in commandTree.Where(x => !x.IsHidden).OrderBy(x => x.Verb))
			{
				var color = node.Command is not null ? ConsoleColor.White : ConsoleColor.DarkCyan;

				output.Write("  ")
					.Write(node.Verb.PadRight(padding), color);
				
				var helpText = (node.Help ?? string.Empty).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				if (helpText.Length == 1)
				{
					output.Write(helpText[0]);
				}
				else
				{
					for (var i = 0; i < helpText.Length; i++)
					{
						if (i > 0)
						{
							output.Write("  ").Write(string.Empty.PadRight(padding));
						}
						output.Write(helpText[i]);
						if (i < helpText.Length - 1)
						{
							output.WriteLine();
						}
					}
				}

				output.WriteLine();
			}
		}


        public void GenerateHelp()
		{
			output.WriteLine("Available commands: ");
			output.WriteLine();

			var padding = commandList.Max(_ => _.ExpandedVerbs.Length) + 4;


			foreach (var command in commandList.Where(x => !x.Hidden).Order())
			{
				output.Write("  ")
					.Write(command.ExpandedVerbs.PadRight(padding), ConsoleColor.DarkCyan);

				var helpText = (command.HelpText ?? string.Empty).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				if (helpText.Length == 1)
				{
					output.Write(helpText[0]);
				}
				else
				{
					for (var i = 0; i < helpText.Length; i++)
					{
						if (i > 0)
						{
							output.Write("  ").Write(string.Empty.PadRight(padding));
						}
						output.Write(helpText[i]);
						if (i < helpText.Length - 1)
						{
							output.WriteLine();
						}
					}
				}

				if (command.Aliases.Count > 0)
				{
					var label = command.Aliases.Count == 1 ? "alias" : "aliases";

					output.Write(" ").Write($"({label}: {string.Join(", ", command.Aliases)})", ConsoleColor.DarkGray);

				}
				output.WriteLine();
			}
		}
	}
}
