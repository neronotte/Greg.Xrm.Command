using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Help
{
	public class HelpGeneratorInMarkdown
	{
		private readonly IOutput output;
		private readonly IReadOnlyList<VerbNode> commandTree;
		private string exportHelpPath;

		public HelpGeneratorInMarkdown(
			IOutput output,
			IReadOnlyList<VerbNode> commandTree,
			string exportHelpPath)
		{
			this.output = output;
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

			CreateSidebar(directory);
		}

		private void CreateReadme(DirectoryInfo directory)
		{
			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
			var fileName = Path.Combine(directory.FullName, $"Home.md");

			this.output.Write($"Generating {fileName}...");
			
			using var writer = new MarkdownWriter(fileName);

			writer.WriteTitle1("Greg.Xrm.Command ⁓ aka PACX");

			writer.WriteParagraph("PACX is a command line tool to interact with Dynamics 365 and Power Platform environments. It can be used to automate tasks that would otherwise require a lot of manual work. It can be also used to perform tasks that are not yet available in the official Dataverse/Power Platform user interface.");

			writer.WriteTitle2("Command Groups");

			var childNamespaces = this.commandTree.Where(x => x.Command is null).ToList();
			writer.WriteTable(childNamespaces,
				() => new[] { "Command group", "Description" },
				child => new[] { $"[**{assemblyName} {child}**]({assemblyName}-{child.ToString().Replace(" ", "-")})",
				Clean(child.Help ?? string.Empty) });

			writer.WriteLine();
		}

		private void CreateSidebar(DirectoryInfo directory)
		{
			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;


			var fileName = Path.Combine(directory.FullName, $"_Sidebar.md");
			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
			{
				writer.WriteTitle3("Command list");

				foreach (var node in this.commandTree.OrderBy(x => x.Verb))
				{
					WriteNode(writer, directory, assemblyName, node, 0);
				}
			}
			this.output.WriteLine("Done", ConsoleColor.Green);
		}

		private void WriteNode(MarkdownWriter writer, DirectoryInfo directory, string assemblyName, VerbNode node, int indent)
		{
			if (node.IsHidden) return;

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
						.Write(node.ToString().Replace(' ', '-'))
						.WriteLine(")");

				CreateCommand(directory, assemblyName, node.Command);
			}
			else
			{
				writer.Write(indentString)
					.Write("- [")
					.Write(assemblyName)
					.Write(" ")
					.Write(node.ToString())
					.Write("](")
					.Write(assemblyName)
					.Write("-")
					.Write(node.ToString().Replace(' ', '-'))
					.WriteLine(")");

				CreateNamespace(directory, assemblyName, node);
			}

			foreach (var child in node.Children.OrderBy(x => x.Verb))
			{
				WriteNode(writer, directory, assemblyName, child, indent + 1);
			}
		}


		private void CreateCommand(DirectoryInfo directory, string assemblyName, CommandDefinition command)
		{
			if (command.Hidden)
				return;

			var fileName = Path.Combine(directory.FullName, $"{assemblyName}-{string.Join("-", command.Verbs)}.md");


			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
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
					writer.WriteLine();
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
						option => new[] {
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

		private void CreateNamespace(DirectoryInfo directory, string assemblyName, VerbNode node)
		{
			var fileName = Path.Combine(directory.FullName, $"{assemblyName}-{node.ToString().Replace(' ', '-')}.md");

			this.output.Write($"Generating {fileName}...");
			using (var writer = new MarkdownWriter(fileName))
			{
				node.WriteNamespaceHelp(writer);

				var childNamespaces = node.Children.Where(x => x.Command is null).ToList();
				if (childNamespaces.Count > 0)
				{
					writer.WriteTitle2("Command Groups");

					writer.WriteTable(childNamespaces,
						() => new[] { "Command Group", "Description" },
						child => new[] { $"[**{assemblyName} {child}**]({assemblyName}-{child.ToString().Replace(" ", "-")})",
						Clean(child.Help ?? string.Empty) });

					writer.WriteLine();
				}

				var childCommands = node.Children.Where(x => x.Command is not null).ToList();
				if (childCommands.Count > 0)
				{
					writer.WriteTitle2("Commands");

					writer.WriteTable(childCommands,
						() => new[] { "Command", "Description" },
						child => new[] { $"[**{assemblyName} {child}**]({assemblyName}-{child.ToString().Replace(" ", "-")})",
						Clean(child.Command?.HelpText ?? string.Empty) });

					writer.WriteLine();
				}
			}
			this.output.WriteLine("Done", ConsoleColor.Green);
		}

		private static string Clean(string v)
		{
			if (string.IsNullOrEmpty(v))
				return v;

			return v.Replace("\n", " ").Replace("\r", " ").Replace("  ", " ");
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

			var nullableProperty = Nullable.GetUnderlyingType(option.Property.PropertyType);
			if (nullableProperty is null)
			{
				return option.Property.PropertyType.Name;
			}


			if (nullableProperty.IsEnum)
			{
				if (option.Option.SuppressValuesHelp)
					return "see description";

				return string.Join(", ", Enum.GetNames(nullableProperty));
			}

			return nullableProperty.Name;
		}
	}
}
