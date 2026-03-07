using Greg.Xrm.Command;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

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
		private const string HelpRequestToken = "/?";

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
			if (!string.IsNullOrWhiteSpace(commandDefinition.HelpText))
			{
				console.Markup($"[{DefaultColors.Text}]Help:[/] {Markup.Escape(commandDefinition.HelpText)}");
				console.WriteLine();
			}
			console.Markup("[dim]Provide values for the command options. Type [yellow]/?[/] for help on an option. CTRL+C to quit.[/]");
			console.WriteLine();

			if (commandDefinition.Options.Count == 0)
			{
				return commandDefinition.CreateCommand(new Dictionary<string, string>());
			}


			console.CreateRule("Command arguments");

			var dict = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var promptText = $"[{DefaultColors.Primary}]?[/] Provide value for [{DefaultColors.Accent}]--{option.Option.LongName}[/]\"";
				if (option.IsRequired)
				{
					promptText += " [red](required)[/]";
				}
				promptText += ":";

				string response;
				do
				{
					var prompt = CreatePrompt(option, promptText);
					response = console.Prompt(prompt);

					if (response == HelpRequestToken)
					{
						ShowOptionHelp(option);
					}
				}
				while (response == HelpRequestToken);

				if (!string.IsNullOrWhiteSpace(response))
				{
					dict["--" + option.Option.LongName] = response;
				}
			}

			var command = commandDefinition.CreateCommand(dict);
			return command;
		}

		private void ShowOptionHelp(OptionDefinition option)
		{
			console.WriteLine();

			var content = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(option.Option.HelpText))
			{
				content.Append($"{Markup.Escape(option.Option.HelpText.Replace(Environment.NewLine, " "))}");
			}
			else
			{
				content.Append($"No help text available for this option.");
			}

			content.AppendLine();
			content.Append($"[{DefaultColors.Text}]Option:[/] [{DefaultColors.Command}]--{Markup.Escape(option.Option.LongName)}[/]");
			if (option.Option.ShortName is not null)
			{
				content.Append($", [{DefaultColors.Command}]-{Markup.Escape(option.Option.ShortName)}[/]");
			}

			if (option.Option.DefaultValue is not null)
			{
				content.AppendLine();
				content.Append($"[{DefaultColors.Text}]Default:[/] [{DefaultColors.Command}]{Markup.Escape(option.Option.DefaultValue.ToString() ?? string.Empty)}[/]");
			}

			content.AppendLine();

			if (option.IsRequired)
			{
				content.Append($"[{Color.Red}]This option is required.[/]");
			}
			else
			{
				content.Append($"[{DefaultColors.Text}]This option is optional.[/]");
			}

			var panelWidth = Math.Max(20, console.Profile.Width / 2);
			var panelWidget = new Panel(new Markup(content.ToString()))
			{
				Width = panelWidth,
				Padding = new Padding(0, 0)
			}
			.Padding(1, 0)
			.BorderColor(Color.Gray)
			.RoundedBorder()
			.Header(" Help ");

			console.Write(panelWidget);
			console.WriteLine();
		}

		private static IPrompt<string> CreatePrompt(OptionDefinition option, string promptText)
		{
			var propertyType = option.Property.PropertyType;

			if (propertyType == typeof(string))
			{
				return CreateTextPrompt(option, promptText);
			}
			if (propertyType == typeof(bool))
			{
				return CreateBoolPrompt(option, promptText);
			}
			if (propertyType.IsEnum)
			{
				return CreateOptionsPrompt(option, promptText, propertyType);
			}

			var underlyingType = Nullable.GetUnderlyingType(propertyType);
			if (underlyingType == null)
			{
				return CreateTextPrompt(option, promptText);
			}
			if (underlyingType.IsEnum)
			{
				return CreateOptionsPrompt(option, promptText, underlyingType);
			}
			if (underlyingType == typeof(bool))
			{
				return CreateBoolPrompt(option, promptText);
			}

			return CreateTextPrompt(option, promptText);
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

			prompt.AddChoice(HelpRequestToken);

			return prompt;
		}

		private static IPrompt<string> CreateBoolPrompt(OptionDefinition option, string promptText)
		{
			var prompt = new SelectionPrompt<string>()
				.Title(promptText)
				.HighlightStyle(new Style(Color.Black, Color.Aquamarine1, Decoration.None))
				.AddChoices("true", "false", HelpRequestToken);

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