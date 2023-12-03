using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using System.Data;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Help
{
    public class HelpGeneratorForVerb
	{
		private readonly IOutput output;
		private readonly VerbNode verb;

		public HelpGeneratorForVerb(IOutput output, VerbNode verb)
        {
			this.output = output;
			this.verb = verb;
		}



		public void GenerateHelp()
		{
			output.Write("Usage: ");
			output.Write(Assembly.GetEntryAssembly()?.GetName().Name, ConsoleColor.DarkCyan);
			output.Write(" ");
			output.Write(verb.ToString(), ConsoleColor.DarkCyan);
			output.Write(" ");

			foreach (var node in verb.Children.Where(x => !x.IsHidden).OrderBy(x => x.Verb))
			{
				var color = node.Command is not null ? ConsoleColor.White : ConsoleColor.DarkCyan;
				output.Write("[", color).Write(node.Verb, color).Write("] ", color);
			}

			output.WriteLine().WriteLine();


			var padding = 28;
			foreach (var node in verb.Children.Where(x => !x.IsHidden).OrderBy(x => x.Verb))
			{
				var color = node.Command is not null ? ConsoleColor.White : ConsoleColor.DarkCyan;

				output.Write("  ")
					.Write(node.Verb.PadRight(padding), color);

				var helpTextString = node.Help ?? string.Empty;
				if (node.Command is not null)
				{
					// the node is a command
					helpTextString = node.Command.HelpText ?? string.Empty;
				}

				var helpText = helpTextString.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

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

				if (node.Command is not null && node.Command.Aliases.Count > 0)
				{
					var label = node.Command.Aliases.Count == 1 ? "alias" : "aliases";

					output.Write(" ").Write($"({label}: {string.Join(", ", node.Command.Aliases)})", ConsoleColor.DarkGray);
				}

				output.WriteLine();
			}
		}
	}
}
