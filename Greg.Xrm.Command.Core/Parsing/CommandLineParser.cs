using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandLineParser
	{
		private readonly List<CommandDefinition> commandDefinitionList = new();
		private readonly CommandTree commandTree = new();
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


#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			var helperType = typeof(INamespaceHelper);
			var namespaceHelpers = (from type in assembly.GetTypes()
									where helperType.IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
									let helper = Activator.CreateInstance(type) as INamespaceHelper
									where helper != null
									select helper).ToList();
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension


			CreateVerbTree(commandList, namespaceHelpers);
		}


		private void CreateVerbTree(IReadOnlyList<CommandDefinition> commandList, List<INamespaceHelper> helpers)
		{
			var list = new List<VerbNode>();

			foreach (var command in commandList.OrderBy(x => x.ExpandedVerbs))
			{
				var nodeList = list;
				for (var i = 0; i < command.Verbs.Count; i++)
				{
					var currentVerbs = command.Verbs.Take(i + 1).ToList();


					var node = nodeList.Find(x => x.Verb == command.Verbs[i]);
					if (node == null)
					{
						node = new VerbNode(command.Verbs[i]);

						if (helpers.Find(x => x.Verbs.SequenceEqual(currentVerbs, StringComparer.OrdinalIgnoreCase)) is INamespaceHelper helper)
						{
							node.Help = helper.GetHelp();
						}

						nodeList.Add(node);
					}

					if (i == command.Verbs.Count - 1)
					{
						node.Command = command;
					}

					nodeList = node.Children;
				}
			}

			this.commandTree.Clear();
			this.commandTree.AddRange(list);
		}




		public object Parse(IEnumerable<string> args)
		{
			return Parse(args.ToArray());
		}



		public object Parse(params string[] args)
		{
			if (!CommandRunArgs.TryParse(args, this.output, out var runArgs))
				return new HelpCommand(this.commandDefinitionList, this.commandTree, runArgs?.Options ?? new Dictionary<string, string>());


			// shows the generic help
			if (runArgs == null 
				|| runArgs.Verbs.Count == 0 
				|| (runArgs.Verbs.Count == 1 && string.Equals("help", runArgs.Verbs[0], StringComparison.OrdinalIgnoreCase)))
			{
				return new HelpCommand(this.commandDefinitionList, this.commandTree, runArgs?.Options ?? new Dictionary<string, string>());
			}


			var showHelp = runArgs.Options.ContainsKey("--help")
				|| runArgs.Options.ContainsKey("-h")
				|| runArgs.Options.ContainsKey("/?");
			

			var commandDefinition = commandDefinitionList.Find(c => c.IsMatch(runArgs.Verbs));
			if (commandDefinition is null)
			{
				var lastMatchingVerb = this.commandTree.FindNode(runArgs.Verbs);
				if (lastMatchingVerb is null)
				{
					this.output.WriteLine("Invalid command", ConsoleColor.Red).WriteLine();

					return new HelpCommand(this.commandDefinitionList, this.commandTree, runArgs?.Options ?? new Dictionary<string, string>());
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
