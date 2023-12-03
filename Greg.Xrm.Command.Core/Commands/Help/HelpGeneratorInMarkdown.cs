using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Help
{
    public class HelpGeneratorInMarkdown
	{
		private readonly IOutput output;
		private readonly IReadOnlyList<CommandDefinition> commandList;
		private readonly IReadOnlyList<VerbNode> commandTree;
		private string exportHelpPath;

		public HelpGeneratorInMarkdown(
			IOutput output, 
			IReadOnlyList<CommandDefinition> commandList, 
			IReadOnlyList<VerbNode> commandTree, 
			string exportHelpPath)
		{
			this.output = output;
			this.commandList = commandList;
			this.commandTree = commandTree;
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

			CreateReadme(directory);

			foreach (var command in this.commandList)
			{
				CreateCommand(directory, command);
			}

			CreateSidebar(directory, this.commandList);
		}

		private void CreateReadme(DirectoryInfo directory)
		{
			var fileName = Path.Combine(directory.FullName, $"Home.md");

			this.output.Write($"Generating {fileName}...");
			using var writer = new MarkdownWriter(fileName);
			writer.WriteTitle1("Greg.Xrm.Command");
		}



		private void CreateCommand(DirectoryInfo directory, CommandDefinition command)
		{
			if (command.Hidden)
				return;

			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

			var fileName = Path.Combine(directory.FullName, $"{assemblyName}-{string.Join("-", command.Verbs)}.md");


			this.output.Write($"Generating {fileName}...");
			using(var writer = new MarkdownWriter(fileName))
			{
				writer.WriteParagraph(command.HelpText);

				if (command.Aliases.Count > 0)
				{
					writer.WriteTitle2("Aliases");
					writer.WriteCodeBlockStart("Console");
					foreach (var alias in command.Aliases)
					{
						writer.Write(assemblyName).Write(" ").Write(alias.ExpandedVerbs).WriteLine();
					}
					writer.WriteCodeBlockEnd();
				}

				if (typeof(ICanProvideUsageExample).IsAssignableFrom(command.CommandType))
				{
					writer.WriteTitle2("Usage");

					var commandImpl = Activator.CreateInstance(command.CommandType) as ICanProvideUsageExample;
					commandImpl?.WriteUsageExamples(writer);
				}

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
			}
			this.output.WriteLine("Done", ConsoleColor.Green);
		}



		private void CreateSidebar(DirectoryInfo directory, IReadOnlyList<CommandDefinition> commandList)
		{
			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;


			var fileName = Path.Combine(directory.FullName, $"_Sidebar.md");
			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
			{
				writer.WriteTitle3("Command list");

				foreach (var node in this.commandTree.OrderBy(x => x.Verb))
				{
					WriteNode(writer, assemblyName, node, 0);
				}
			}
			this.output.WriteLine("Done", ConsoleColor.Green);
		}

		private void WriteNode(MarkdownWriter writer, string assemblyName, VerbNode node, int indent)
		{
			var indentString = indent == 0 ? string.Empty : new string(' ', indent * 2);
			if (node.Command is not null)
			{
				writer.Write(indentString)
					.Write("- [")
					.Write(assemblyName)
					.Write(" ")
					.Write(node.ToString())
					.Write("](")
					.Write(assemblyName)
					.Write("-")
					.Write(node.Verb.Replace(' ', '-'))
					.WriteLine(")");
			}
			else
			{
				writer.Write(indentString)
					.Write("- ")
					.Write(assemblyName)
					.Write(" ")
					.Write(node.Verb)
					.WriteLine();
			}

			foreach (var child in node.Children.OrderBy(x => x.Verb))
			{
				WriteNode(writer, assemblyName, child, indent + 1);
			}
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
