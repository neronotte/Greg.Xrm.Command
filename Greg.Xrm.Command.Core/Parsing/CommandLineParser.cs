using Greg.Xrm.Command.Commands;
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
							   select new CommandDefinition(commandAttribute, commandType)).ToList();
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension


			commandDefinitionList.AddRange(commandList);
		}





		public object? Parse(IEnumerable<string> args)
		{
			return Parse(args.ToArray());
		}

		public object? Parse(params string[] args)
		{
			var verbs = new List<string>();
			var options = new Dictionary<string, string>();

			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (IsVerb(arg, options))
				{
					verbs.Add(arg);
				}
				else if (IsOption(arg))
				{
					var optionName = arg;
					if (i + 1 >= args.Length)
					{
						options.Add(optionName, string.Empty);
						continue;
					}


					var optionValue = args[i + 1];
					if (IsOption(optionValue))
					{
						options.Add(optionName, string.Empty);
						continue;
					}
					
					options.Add(optionName, optionValue);
					i++; // need to advance by two
				}
				else
				{
					output.WriteLine($"Invalid syntax on argument '{arg}'. Type --help to get help on a specific command syntax.");
					return null;
				}
			}


			// shows the generic help
			if (verbs.Count == 0 || (verbs.Count == 1 && string.Equals("help", verbs[0], StringComparison.OrdinalIgnoreCase)))
			{
				return new HelpCommand(this.commandDefinitionList);
			}


			var showHelp = options.ContainsKey("--help")
				|| options.ContainsKey("-h")
				|| options.ContainsKey("/?");
			

			var commandDefinition = commandDefinitionList.Find(c => c.IsMatch(verbs));
			if (commandDefinition is null) 
				return null;

			if (showHelp)
			{
				return new HelpCommand(commandDefinition);
			}


			var command = commandDefinition.CreateCommand(options);
			return command;
		}




		private static bool IsOption(string arg)
		{
			return arg.StartsWith("-", StringComparison.Ordinal);
		}

		private static bool IsVerb(string arg, Dictionary<string, string> options)
		{
			return options.Count == 0 && !IsOption(arg);
		}
	}
}
