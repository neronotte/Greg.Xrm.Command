using Greg.Xrm.Command;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Parsing.Attributes;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Data;
using System.Reflection;
using Rule = Spectre.Console.Rule;

namespace Greg.Xrm.Command.Interactive
{
	class CommandRunnerInteractive(
		IOutput output,
		IAnsiConsole console,
		ILogger log,
		ICommandRegistry commandRegistry,
		ICommandExecutorFactory commandExecutorFactory,
		IHistoryTracker historyTracker,
		ICommandLineArguments args) :
		CommandRunnerBase(output, log, commandExecutorFactory, historyTracker, args), ICommandRunner
	{
		public async Task<int> RunCommandAsync(CancellationToken cancellationToken)
		{
			var commandTree = commandRegistry.Tree;
			var commandDefinition = VerbTreeRecourser.Recourse(console, commandTree);

			if (commandDefinition is null) return -1;

			var command = TryInitializeCommand(commandDefinition);

			if (command is null) return -1;
			if (!IsValidCommand(command)) return -1;

			await TrackCommandIntoHistoryAsync(command);

			console.CreateRule("Command execution");

			var result = await base.ExecuteCommand(command, cancellationToken);
			
			return result;
		}






		





		object TryInitializeCommand(CommandDefinition commandDefinition)
		{
			var options = commandDefinition.Options;

			console.Markup($"[{DefaultColors.Text}]Selected command:[/] [{DefaultColors.Command}]{commandDefinition.ExpandedVerbs}[/]");
			console.WriteLine();
			if (commandDefinition.Options.Count == 0)
			{
				return commandDefinition.CreateCommand(new Dictionary<string, string>());
			}


			console.CreateRule("Command arguments");

			var dict = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var promptText = $"[{DefaultColors.Primary}]?[/] Provide value for argument [{DefaultColors.Accent}]--{option.Option.LongName}[/]\"";
				if (option.IsRequired)
				{
					promptText += " [red](required)[/]";
				}

				//if (!string.IsNullOrWhiteSpace(option.Option.HelpText))
				//{
				//	promptText += $"{Environment.NewLine}[{DefaultColors.Text}]{Markup.Escape(option.Option.HelpText)}[/]";
				//}
				//promptText += $"{Environment.NewLine}[cyan]>[/] ";
				promptText += ":";

				var propertyType = option.Property.PropertyType;

				IPrompt<string> prompt;
				if (propertyType == typeof(string))
				{
					prompt = CreateTextPrompt(option, promptText);
				}
				else if (propertyType == typeof(bool))
				{
					prompt = CreateBoolPrompt(option, promptText);
				}
				else if (propertyType.IsEnum)
				{
					prompt = CreateOptionsPrompt(option, promptText, propertyType);
				}
				else
				{
					var underlyingType = Nullable.GetUnderlyingType(propertyType);
					if (underlyingType == null)
					{
						prompt = CreateTextPrompt(option, promptText);
					}
					else if (underlyingType.IsEnum)
					{
						prompt = CreateOptionsPrompt(option, promptText, underlyingType);
					}
					else if (underlyingType == typeof(bool))
					{
						prompt = CreateBoolPrompt(option, promptText);
					}
					else
					{
						prompt = CreateTextPrompt(option, promptText);
					}
				}

				var response = console.Prompt(prompt);

				if (!string.IsNullOrWhiteSpace(response))
				{
					dict["--" + option.Option.LongName] = response;
				}
			}

			var command = commandDefinition.CreateCommand(dict);
			return command;
		}

		private static IPrompt<string> CreateOptionsPrompt(OptionDefinition option, string promptText, Type enumType)
		{
			var choices = enumType.GetEnumNames();

			var prompt = new SelectionPrompt<string>()
				.Title(promptText)
				.EnableSearch()
				.SearchPlaceholderText("Type to search...")
				.HighlightStyle(new Style(Color.Black, Color.Aquamarine1, Decoration.None))
				.AddChoices(choices);

			return prompt;
		}

		private static IPrompt<string> CreateBoolPrompt(OptionDefinition option, string promptText)
		{
			var prompt = new SelectionPrompt<string>()
				.Title(promptText)
				.HighlightStyle(new Style(Color.Black, Color.Aquamarine1, Decoration.None))
				.AddChoices("true", "false");

			return prompt;
		}

		private static IPrompt<string> CreateTextPrompt(OptionDefinition option, string promptText)
		{
			var p = new TextPrompt<string>(promptText);

			if (!option.IsRequired)
			{
				p = p.AllowEmpty();
			}
			if (option.Option.DefaultValue is not null)
			{
				p = p.DefaultValue(option.Option.DefaultValue?.ToString() ?? string.Empty);
			}
			return p;
		}
	}
}