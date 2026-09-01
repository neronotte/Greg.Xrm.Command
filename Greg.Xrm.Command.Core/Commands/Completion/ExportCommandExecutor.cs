using Greg.Xrm.Command.Parsing;
using Newtonsoft.Json;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Completion
{
	public class ExportCommandExecutor(
		IAnsiConsole ansiConsole,
		ICommandRegistry registry
		) : ICommandExecutor<ExportCommand>
	{
		public Task<CommandResult> ExecuteAsync(ExportCommand command, CancellationToken cancellationToken)
		{
			var commands = registry.Commands
				.Where(c => !c.Hidden)
				.OrderBy(c => c)
				.Select(c => new
				{
					verbs = c.Verbs,
					aliases = c.Aliases.Select(a => a.Verbs).ToArray(),
					help = c.HelpText,
					options = c.Options.Select(o => new
					{
						@long = o.Option.LongName,
						@short = o.Option.ShortName,
						help = o.Option.HelpText,
						required = o.IsRequired,
						@default = o.Option.DefaultValue?.ToString(),
						values = GetEnumValues(o)
					}).ToArray()
				})
				.ToArray();

			var namespaces = new List<object>();
			CollectNamespaces(registry.Tree, new List<string>(), namespaces);

			var json = JsonConvert.SerializeObject(new { commands, namespaces }, Formatting.Indented);

			// written through the raw output writer on purpose: the ansi console
			// renderer would hard-wrap long lines at the console width, which
			// breaks consumers that parse the JSON (e.g. the completion scripts)
			ansiConsole.Profile.Out.Writer.WriteLine(json);

			return Task.FromResult(CommandResult.Success());
		}


		private static string[]? GetEnumValues(OptionDefinition optionDefinition)
		{
			var enumType = optionDefinition.Property.PropertyType.GetEnumType();
			if (enumType == null || optionDefinition.Option.SuppressValuesHelp)
				return null;

			return Enum.GetNames(enumType);
		}


		private static void CollectNamespaces(IReadOnlyList<VerbNode> nodes, List<string> path, List<object> result)
		{
			foreach (var node in nodes)
			{
				if (node.IsHidden) continue;
				if (node.Children.Count == 0) continue;

				var currentPath = new List<string>(path) { node.Verb };
				result.Add(new
				{
					verbs = currentPath.ToArray(),
					help = node.Help
				});

				CollectNamespaces(node.Children, currentPath, result);
			}
		}
	}
}
