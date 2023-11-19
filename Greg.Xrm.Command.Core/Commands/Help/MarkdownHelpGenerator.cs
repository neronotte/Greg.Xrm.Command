using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;
using System.Text;

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

			CreateReadme(directory);

			foreach (var command in this.commandList)
			{
				CreateCommand(directory, command);
			}

			CreateSidebar(directory, this.commandList);
		}

		private void CreateReadme(DirectoryInfo directory)
		{
			var fileName = Path.Combine(directory.FullName, $"README.md");

			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
			{
				writer.WriteTitle1("Greg.Xrm.Command");
			}
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



		private void CreateSidebar(DirectoryInfo directory, IReadOnlyList<CommandDefinition> commandList)
		{
			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;


			var fileName = Path.Combine(directory.FullName, $"_Sidebar.md");
			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
			{
				writer.WriteTitle3("Command list");
				var tree = CreateVerbTree(commandList);


				foreach (var node in tree.OrderByDescending(x => x.Verb))
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
					.Write(node.Verb)
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

			foreach (var child in node.Children.OrderByDescending(x => x.Verb))
			{
				WriteNode(writer, assemblyName, child, indent + 1);
			}
		}

		private List<VerbNode> CreateVerbTree(IReadOnlyList<CommandDefinition> commandList)
		{
			var list = new List<VerbNode>();

			foreach (var command in commandList.OrderByDescending(x => x.ExpandedVerbs))
			{
				var nodeList = list;
				for (var i = 0; i < command.Verbs.Count; i++)
				{
					var node = nodeList.Find(x => x.Verb == command.Verbs[i]);
					if (node == null)
					{
						node = new VerbNode(Name(command.Verbs, i));
						nodeList.Add(node);
					}

					if (i == command.Verbs.Count - 1)
					{
						node.Command = command;
					}

					nodeList = node.Children;
				}
			}
			return list;
		}

		private string Name(IReadOnlyList<string> verbs, int index)
		{
			var sb = new StringBuilder();

			for (int i = 0; i <= index; i++)
			{
				sb.Append(verbs[i]).Append(' ');
			}

			return sb.ToString().TrimEnd();
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


		class VerbNode
		{
            public VerbNode(string verb)
            {
				this.Verb = verb;
			}

            public string Verb { get; set; }

			public List<VerbNode> Children { get; } = new List<VerbNode>();

			public CommandDefinition? Command { get; set; }
		}
	}
}
