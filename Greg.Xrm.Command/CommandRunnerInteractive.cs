using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Data;

namespace Greg.Xrm.Command
{
	class CommandRunnerInteractive(
		IOutput output,
		IAnsiConsole console,
		ILogger log,
		ICommandRegistry commandRegistry,
		ICommandParser parser,
		ICommandExecutorFactory commandExecutorFactory,
		IHistoryTracker historyTracker,
		ICommandLineArguments args) :
		CommandRunnerBase(output, log, commandExecutorFactory, historyTracker, args), ICommandRunner
	{
		public async Task<int> RunCommandAsync(CancellationToken cancellationToken)
		{
			var commandTree = commandRegistry.Tree;
			var commandDefinition = Recourse(commandTree);

			if (commandDefinition is null) return -1;

			var command = TryInitializeCommand(commandDefinition);

			var result = await base.ExecuteCommand(command!, cancellationToken);
			
			return result;
		}


		object TryInitializeCommand(CommandDefinition commandDefinition)
		{
			var options = commandDefinition.Options;

			var dict = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var promptText = $"[cyan]?[/] Provide value for argument \"[green]{option.Option.LongName}[/]\"";
				if (option.IsRequired)
				{
					promptText += " [red](required)[/]";
				}

				if (!string.IsNullOrWhiteSpace(option.Option.HelpText))
				{
					promptText += $"{Environment.NewLine}[grey]{Markup.Escape(option.Option.HelpText)}[/]";
				}
				promptText +=$"{Environment.NewLine}[cyan]>[/] ";

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

				console.WriteLine();
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




		private CommandDefinition? Recourse(IEnumerable<VerbNode> tree)
		{
			VerbNode result;
			do
			{
				var maxLenght = tree.Max(node => node.Verb.Length);

				var prompt = new SelectionPrompt<VerbNode>()
					.Title("Select [cyan]namespace[/] or [white]command[/]:")
					.WrapAround()
					.EnableSearch()
					.SearchPlaceholderText("Type to search...")
					.HighlightStyle(new Style(Color.Black, Color.Aquamarine1, Decoration.None))
					.UseConverter(node => $"[{GetColor(node)}]{node.Verb.PadRight(maxLenght)}[/] - {Normalize(node)}")
					.AddChoices(tree.Where(x => !x.IsHidden));

				result = console.Prompt(prompt);
				tree = result.Children;
			}
			while (result.Command is null);

			return result.Command;
		}





		static string GetColor(VerbNode node)
		{
			return node.Command is null ? "cyan" : "white";
		}

		static string? Normalize(VerbNode node)
		{
			var text = node.Command is null ? node.Help : node.Command.HelpText;

			text = text?
				.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrEmpty(line))
				.FirstOrDefault();
				//.Aggregate((a, b) => $"{a} {b}");

			return text != null ? Markup.Escape(text) : null;
		}
	}
}