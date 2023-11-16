using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandRunArgs
	{
		public CommandRunArgs(List<string> verbs, Dictionary<string, string> options)
		{
			this.Verbs = verbs;
			this.Options = options;
		}

		public IReadOnlyList<string> Verbs { get; }
		public IReadOnlyDictionary<string, string> Options { get; }







		public static bool TryParse(string[] args, IOutput output, out CommandRunArgs? result)
		{
			result = null;

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
					return false;
				}
			}

			result = new CommandRunArgs(verbs, options);
			return true;
		}

		private static bool IsOption(string arg)
		{
			if (double.TryParse(arg, out double value)) return false;
			return arg.StartsWith("-", StringComparison.Ordinal);
		}

		private static bool IsVerb(string arg, Dictionary<string, string> options)
		{
			return options.Count == 0 && !IsOption(arg);
		}
	}
}
