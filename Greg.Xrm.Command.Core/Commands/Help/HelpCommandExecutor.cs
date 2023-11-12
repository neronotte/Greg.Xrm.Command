using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Help
{
	public class HelpCommandExecutor : ICommandExecutor<HelpCommand>
	{
		private readonly IOutput output;

		public HelpCommandExecutor(IOutput output)
		{
			this.output = output;
		}


		public Task ExecuteAsync(HelpCommand command, CancellationToken cancellationToken)
		{
			if (command.CommandDefinition is null)
			{
				ShowGenericHelp(command.CommandList);
				return Task.CompletedTask;
			}



			var commandAttribute = command.CommandDefinition.CommandType.GetCustomAttribute<CommandAttribute>();
			if (commandAttribute == null)
				return Task.CompletedTask;

			if (!string.IsNullOrWhiteSpace(commandAttribute.HelpText))
			{
				output.WriteLine();
				output.WriteLine(commandAttribute.HelpText);
			}

			output.Write("Usage: ");
			output.Write(Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty, ConsoleColor.DarkCyan);
			output.Write(" ");
			foreach (var verb in command.CommandDefinition.Verbs)
			{
				output.Write(verb, ConsoleColor.DarkCyan).Write(" ");
			}
			foreach (var optionDefinition in command.CommandDefinition.Options)
			{
				output.Write($"[--{optionDefinition.Option.LongName}] ", ConsoleColor.DarkCyan);
			}
			output.WriteLine().WriteLine();





			var padding = command.CommandDefinition.Options.Max(_ => _.Option.LongName.Length) + 6;
			foreach (var optionDef in command.CommandDefinition.Options)
			{
				var option = optionDef.Option;
				var prop = optionDef.Property;

				output
					.Write("  ")
					.Write($"--{option.LongName}".PadRight(padding, ' '), ConsoleColor.DarkCyan);

				if (!option.IsRequired)
				{
					output.Write("[optional] ", ConsoleColor.DarkGray);
				}
				else
				{
					output.Write("[required] ", ConsoleColor.DarkRed);
				}





				if (option.HelpText != null)
				{
					var helpText = (option.HelpText ?? string.Empty).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

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
								output.WriteLine().Write("  ").Write(string.Empty.PadRight(padding+11));
							}
							output.Write(helpText[i]);
						}
					}
					output.Write(" ");
				}
				if (option.ShortName != null)
				{
					output.Write($"(alias: -{option.ShortName}) ");
				}
				if (option.DefaultValue != null)
				{
					output.WriteLine().Write("  ").Write(string.Empty.PadRight(padding + 11));
					output.Write($"[default: {option.DefaultValue}] ", ConsoleColor.DarkGray);
				}
				if (prop.PropertyType.IsEnum && !option.SuppressValuesHelp)
				{
					output.WriteLine().Write("  ").Write(string.Empty.PadRight(padding+11));
					output.Write($"[values: {string.Join(", ", Enum.GetNames(prop.PropertyType))}] ", ConsoleColor.DarkGray);
				}
				output.WriteLine();
			}

			output.WriteLine();
			return Task.CompletedTask;
		}




		private void ShowGenericHelp(List<CommandDefinition> commandList)
		{
			output.WriteLine("Available commands: ");
			output.WriteLine();

			var padding = commandList.Max(_ => _.ExpandedVerbs.Length) + 4;
			

			foreach (var command in commandList.Order())
			{
				output.Write("  ")
					.Write(command.ExpandedVerbs.PadRight(padding), ConsoleColor.DarkCyan);

				var helpText = (command.HelpText ?? string.Empty).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				if (helpText.Length == 1)
				{
					output.WriteLine(helpText[0]);
				}
				else
				{
					for (var i = 0; i < helpText.Length; i++)
					{
						if (i > 0)
						{
							output.Write("  ").Write(string.Empty.PadRight(padding));
						}
						output.WriteLine(helpText[i]);
					}
				}
			}
		}
	}
}
