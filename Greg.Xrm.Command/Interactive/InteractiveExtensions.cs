using Greg.Xrm.Command.Parsing;
using Spectre.Console;

namespace Greg.Xrm.Command.Interactive
{

	static class InteractiveExtensions
	{


		public static string GetPromptText(this OptionDefinition option)
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
			promptText += $"{Environment.NewLine}[cyan]>[/] ";
			return promptText;
		}


		public static void CreateRule(this IAnsiConsole console, string title)
		{
			console.Write(new Rule($"[{DefaultColors.Primary}]{title}[/]")
			{
				Style = Style.Parse(DefaultColors.Primary),
				Border = BoxBorder.Rounded
			});
			console.WriteLine();
		}
	}
}
