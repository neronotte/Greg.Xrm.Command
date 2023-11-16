using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandLineParser
	{
		private readonly List<CommandDefinition> commandDefinitionList = new();
		private readonly IOutput output;

		public CommandLineParser(IOutput output)
        {
			this.output = output;
		}


        public void InitializeFromAssembly(Assembly assembly)
		{
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			var commandList = (from commandType in assembly.GetTypes()
							   let commandAttribute = commandType.GetCustomAttribute<CommandAttribute>()
							   where commandAttribute != null
							   where !commandType.IsAbstract && commandType.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
							   where !commandDefinitionList.Exists(c => c.CommandType == commandType)
							   let aliasAttributes = (commandType.GetCustomAttributes<AliasAttribute>()?.ToArray() ?? Array.Empty<AliasAttribute>())
							   select new CommandDefinition(commandAttribute, commandType, aliasAttributes)).ToList();
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension


			foreach (var command in commandList)
			{
				foreach (var command2 in this.commandDefinitionList)
				{
					if (command.TryMatch(command2, out var matchedAlias))
						throw new CommandException(CommandException.DuplicateCommand, $"Duplicate command {matchedAlias}.");
				}

				this.commandDefinitionList.Add(command);
			}
		}





		public object? Parse(IEnumerable<string> args)
		{
			return Parse(args.ToArray());
		}



		public object? Parse(params string[] args)
		{
			if (!CommandRunArgs.TryParse(args, this.output, out var runArgs))
				return null;


			// shows the generic help
			if (runArgs == null 
				|| runArgs.Verbs.Count == 0 
				|| (runArgs.Verbs.Count == 1 && string.Equals("help", runArgs.Verbs[0], StringComparison.OrdinalIgnoreCase)))
			{
				return new HelpCommand(this.commandDefinitionList, runArgs.Options);
			}


			var showHelp = runArgs.Options.ContainsKey("--help")
				|| runArgs.Options.ContainsKey("-h")
				|| runArgs.Options.ContainsKey("/?");
			

			var commandDefinition = commandDefinitionList.Find(c => c.IsMatch(runArgs.Verbs));
			if (commandDefinition is null) 
				return null;

			if (showHelp)
			{
				return new HelpCommand(commandDefinition);
			}


			var command = commandDefinition.CreateCommand(runArgs.Options);
			return command;
		}



	}
}
