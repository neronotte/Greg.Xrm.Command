using Autofac;
using Autofac.Core;
using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandRegistry : ICommandRegistry
	{
		private readonly List<CommandDefinition> commandDefinitionList = new();
		private readonly CommandTree commandTree = new();
		private readonly List<IModule> moduleDefinitions = new();
		private readonly ILifetimeScope container;


		public CommandRegistry(ILifetimeScope container)
		{
			this.container = container;
		}


		public CommandTree Tree => this.commandTree;

		public IReadOnlyList<CommandDefinition> Commands => this.commandDefinitionList;

		public IReadOnlyList<IModule> Modules => this.moduleDefinitions;







		public void InitializeFromAssembly(Assembly assembly)
		{
			ScanForModules(assembly);
			var commandList = ScanForCommands(assembly);
			var namespaceHelpers = ScanForNamespaceHelpers(assembly);
			CreateVerbTree(commandList, namespaceHelpers);
		}






		private void ScanForModules(Assembly assembly)
		{
			var moduleType = typeof(IModule);
			var moduleList = (from type in assembly.GetTypes()
							  where moduleType.IsAssignableFrom(type) && !type.IsAbstract && type.GetCustomAttribute<ObsoleteAttribute>() == null
							  let module = this.container.ResolveOptional(type) as IModule
							  where module != null
							  select module).ToList();

			this.moduleDefinitions.AddRange(moduleList);
		}






		private List<CommandDefinition> ScanForCommands(Assembly assembly)
		{
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			var commandList = (from commandType in assembly.GetTypes()
							   let commandAttribute = commandType.GetCustomAttribute<CommandAttribute>()
							   where commandAttribute != null
							   where !commandType.IsAbstract && commandType.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
							   where !commandDefinitionList.Exists(c => c.CommandType == commandType)
							   let aliasAttributes = (commandType.GetCustomAttributes<AliasAttribute>()?.ToArray() ?? Array.Empty<AliasAttribute>())
							   select new CommandDefinition(commandAttribute, commandType, aliasAttributes)).ToList();
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension

			foreach (var command in commandList)
			{
				foreach (var command2 in this.commandDefinitionList)
				{
					if (command.TryMatch(command2, out var matchedAlias))
						throw new CommandException(CommandException.DuplicateCommand, $"Duplicate command {matchedAlias}.");
				}

				this.commandDefinitionList.Add(command);
			}

			return commandList;
		}

		private List<INamespaceHelper> ScanForNamespaceHelpers(Assembly assembly)
		{
			var helperType = typeof(INamespaceHelper);
			var namespaceHelpers = (from type in assembly.GetTypes()
									where helperType.IsAssignableFrom(type) && !type.IsAbstract
									let helper = this.container.ResolveOptional(type) as INamespaceHelper
									where helper != null
									select helper).ToList();

			return namespaceHelpers;
		}





		private void CreateVerbTree(IReadOnlyList<CommandDefinition> commandList, List<INamespaceHelper> helpers)
		{
			var list = new List<VerbNode>();

			foreach (var command in commandList.OrderBy(x => x.ExpandedVerbs))
			{
				VerbNode? parent = null;
				var nodeList = list;
				for (var i = 0; i < command.Verbs.Count; i++)
				{
					var currentVerbs = command.Verbs.Take(i + 1).ToList();


					var node = nodeList.Find(x => x.Verb == command.Verbs[i]);
					if (node == null)
					{
						var helper = helpers.Find(x => x.Verbs.SequenceEqual(currentVerbs, StringComparer.OrdinalIgnoreCase)) ?? NamespaceHelper.Empty;
						node = new VerbNode(command.Verbs[i], parent, helper);
						nodeList.Add(node);
					}

					if (i == command.Verbs.Count - 1)
					{
						node.Command = command;
					}

					nodeList = node.Children;
					parent = node;
				}
			}

			this.commandTree.Clear();
			this.commandTree.AddRange(list);
		}
	}
}
