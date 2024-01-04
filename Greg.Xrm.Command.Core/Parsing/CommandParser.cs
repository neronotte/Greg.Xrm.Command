using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Parsing
{
    public class CommandParser : ICommandParser
	{
		private readonly IOutput output;
		private readonly ICommandRegistry registry;

		public CommandParser(IOutput output, ICommandRegistry registry)
        {
			this.output = output;
			this.registry = registry;
		}


		public object Parse(IEnumerable<string> args)
		{
			return Parse(args.ToArray());
		}



		public object Parse(params string[] args)
		{
			if (!CommandRunArgs.TryParse(args, this.output, out var runArgs))
				return new HelpCommand(this.registry.Commands, this.registry.Tree, runArgs?.Options ?? new Dictionary<string, string>());


			// shows the generic help
			if (runArgs == null 
				|| runArgs.Verbs.Count == 0 
				|| (runArgs.Verbs.Count == 1 && string.Equals("help", runArgs.Verbs[0], StringComparison.OrdinalIgnoreCase)))
			{
				return new HelpCommand(this.registry.Commands, this.registry.Tree, runArgs?.Options ?? new Dictionary<string, string>());
			}


			var showHelp = runArgs.Options.ContainsKey("--help")
				|| runArgs.Options.ContainsKey("-h")
				|| runArgs.Options.ContainsKey("/?");
			

			var commandDefinition = this.registry.Commands.FirstOrDefault(c => c.IsMatch(runArgs.Verbs));
			if (commandDefinition is null)
			{
				var lastMatchingVerb = this.registry.Tree.FindNode(runArgs.Verbs);
				if (lastMatchingVerb is null)
				{
					this.output.WriteLine("Invalid command", ConsoleColor.Red).WriteLine();

					return new HelpCommand(this.registry.Commands, this.registry.Tree, runArgs?.Options ?? new Dictionary<string, string>());
				}
				else
				{
					return new HelpCommand(lastMatchingVerb);
				}
			}


			if (showHelp)
			{
				return new HelpCommand(commandDefinition);
			}


			var command = commandDefinition.CreateCommand(runArgs.Options);
			return command;
		}
	}
}
