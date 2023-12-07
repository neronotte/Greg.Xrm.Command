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


		public Task<CommandResult> ExecuteAsync(HelpCommand command, CancellationToken cancellationToken)
		{
			if (command.ExportHelp)
			{
				this.output.WriteLine("Generating help files...");
				
				var generator = new HelpGeneratorInMarkdown(this.output, command.CommandList, command.CommandTree, command.ExportHelpPath);
				generator.GenerateMarkdownHelp();

				return Task.FromResult(CommandResult.Success());
			}

			if (command.LastMatchingVerb is not null)
			{
				var generator = new HelpGeneratorForVerb(this.output, command.LastMatchingVerb);
				generator.GenerateHelp();

				return Task.FromResult(CommandResult.Success());
			}

			if (command.CommandDefinition is null)
			{
				var generator = new HelpGeneratorGeneric(this.output, command.CommandList, command.CommandTree);
				generator.GenerateHelp2();

				return Task.FromResult(CommandResult.Success());
			}







			var commandAttribute = command.CommandDefinition.CommandType.GetCustomAttribute<CommandAttribute>();
			if (commandAttribute == null)
				return Task.FromResult(CommandResult.Success());

			if (!string.IsNullOrWhiteSpace(commandAttribute.HelpText))
			{
				output.WriteLine();
				output.WriteLine(commandAttribute.HelpText.Replace("\n", " "));
				output.WriteLine();
			}

			output.Write("Usage: ");
			output.Write(Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty, ConsoleColor.DarkCyan);
			output.Write(" ");
			for (int i = 0; i < command.CommandDefinition.Verbs.Count; i++)
			{
				var verb = command.CommandDefinition.Verbs[i];
				output.Write(verb, i < command.CommandDefinition.Verbs.Count - 1 ? ConsoleColor.DarkCyan : ConsoleColor.White).Write(" ");
			}
			foreach (var optionDefinition in command.CommandDefinition.Options)
			{
				output.Write($"[--{optionDefinition.Option.LongName}] ", optionDefinition.IsRequired ? ConsoleColor.DarkRed : ConsoleColor.DarkGray);
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

				if (optionDef.IsRequired)
				{
					output.Write("[required] ", ConsoleColor.DarkRed);
				}
				else
				{
					output.Write("[optional] ", ConsoleColor.DarkGray);
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

				var enumType = prop.PropertyType.GetEnumType();
				if (enumType != null && !option.SuppressValuesHelp)
				{
					output.WriteLine().Write("  ").Write(string.Empty.PadRight(padding+11));
					output.Write($"[values: {string.Join(", ", Enum.GetNames(enumType))}] ", ConsoleColor.DarkGray);
				}
				output.WriteLine();
			}

			output.WriteLine();
			return Task.FromResult(CommandResult.Success());
		}
	}
}
