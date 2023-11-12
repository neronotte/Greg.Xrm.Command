using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Help
{
	public class MarkdownHelpGenerator
	{
		private readonly IOutput output;
		private readonly IReadOnlyList<CommandDefinition> commandList;
		private string exportHelpPath;

		public MarkdownHelpGenerator(IOutput output, IReadOnlyList<CommandDefinition> commandList, string exportHelpPath)
		{
			this.output = output;
			this.commandList = commandList;
			this.exportHelpPath = exportHelpPath;
		}


		public void GenerateMarkdownHelp()
		{
			if (string.IsNullOrWhiteSpace(this.exportHelpPath))
			{
				this.exportHelpPath = Path.Combine(Environment.CurrentDirectory, "help");
			}

			this.output.WriteLine("Generating help files into " + this.exportHelpPath);
				

			var directory = new DirectoryInfo(this.exportHelpPath);
			if (!directory.Exists)
				directory.Create();

			foreach (var command in this.commandList)
			{
				CreateCommand(directory, command);
			}
		}

		private void CreateCommand(DirectoryInfo directory, CommandDefinition command)
		{
			if (command.Hidden)
				return;

			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

			var fileName = Path.Combine(directory.FullName, $"{assemblyName}-{string.Join("-", command.Verbs)}.md");


			this.output.Write($"Generating {fileName}...");
			using(var writer = new MarkdownWriter(fileName))
			{
				writer.WriteParagraph(command.HelpText);


				if (command.Options.Count > 0)
				{
					writer.WriteTitle2("Arguments");
					writer.WriteTable(command.Options, 
						() => new[] { "Long Name", "Short Name", "Required?", "Description", "Default value", "Valid values" },
						option => new [] {
							option.Option.LongName.ToMarkdownCode(), 
							option.Option.ShortName.ToMarkdownCode(),
							option.IsRequired ? "Y" : "N",
							option.Option.HelpText?.Replace("\n", " ") ?? string.Empty,
							option.Option.DefaultValue?.ToString().ToMarkdownCode() ?? "-",
							GetValuesFor(option)
						});
				}

				if (typeof(ICanProvideUsageExample).IsAssignableFrom(command.CommandType))
				{
					writer.WriteTitle2("Usage");

					var commandImpl = Activator.CreateInstance(command.CommandType) as ICanProvideUsageExample;
					commandImpl?.WriteUsageExamples(writer);
				}
			}
			this.output.WriteLine("Done", ConsoleColor.Green);
		}









		private static string GetValuesFor(OptionDefinition option)
		{
			if (option.Property.PropertyType == typeof(bool))
				return "true, false";

			if (option.Property.PropertyType.IsEnum)
			{
				if (option.Option.SuppressValuesHelp)
					return "see description";

				return string.Join(", ", Enum.GetNames(option.Property.PropertyType));
			}
				

			return option.Property.PropertyType.Name;
		}
	}
}
