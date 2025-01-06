using Autofac;
using Autofac.Core;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandRegistry : ICommandRegistry
	{
		private readonly List<CommandDefinition> commandDefinitionList = new();
		private readonly List<INamespaceHelper> namespaceHelperList = new();
		private readonly CommandTree commandTree = new();
		private readonly List<IModule> moduleDefinitions = new();
		private readonly ILogger<CommandRegistry> log;
		private readonly IOutput output;
		private readonly IStorage storage;

		public CommandRegistry(
			ILogger<CommandRegistry> log,
			IOutput output,
			IStorage storage)
		{
			this.log = log;
			this.output = output;
			this.storage = storage;
		}


		public ICommandTree Tree => this.commandTree;

		public IReadOnlyList<CommandDefinition> Commands => this.commandDefinitionList;

		public IReadOnlyList<IModule> Modules => this.moduleDefinitions;




		public void ScanPluginsFolder(ICommandLineArguments args)
		{
			var loaders = new List<(string, PluginLoader, List<string>)>();

			var indexOfPluginsArgument = args.IndexOf("--plugin");
			if (indexOfPluginsArgument < 0)
			{
				var storageFolder = this.storage.GetOrCreateStorageFolder();
				var pluginStorageDirectory = storageFolder.GetDirectories("Plugins").FirstOrDefault();
				if (pluginStorageDirectory == null)
				{
					this.log.LogInformation("No plugins folder found under <{StorageFolder}> No plugins will be loaded.", storageFolder.FullName);
					return;
				}


				// each plugin is stored in a subdirectory
				// the name of the subdirectory is the name of the plugin
				foreach (var pluginFolder in pluginStorageDirectory.GetDirectories())
				{
					this.log.LogDebug("Creating loader for plugin {PluginName}...", pluginFolder.Name);
					try
					{
						// checks if the current plugin folder is marked for deletion
						// if so, deletes the folder and skips the plugin
						if (CheckDeletionMark(pluginFolder)) continue;


						// checks if the plugin folder contains a DLL with the same name. Otherwise is not a valid plugin folder
						var assemblyFile = pluginFolder.GetFiles(pluginFolder.Name + ".dll").FirstOrDefault();
						if (assemblyFile == null)
						{
							log.LogWarning("Plugin {PluginName} does not contain a DLL with the same name.", pluginFolder.Name);
							continue;
						}

						var loader = PluginLoader.CreateFromAssemblyFile(assemblyFile.FullName,
							config => config.PreferSharedTypes = true);

						loaders.Add((pluginFolder.Name, loader, []));
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Error while creating loader for plugin {PluginName}: {Message}", pluginFolder.Name, ex.Message);
					}
				}




			}
			else
			{
				if (args.Count <= indexOfPluginsArgument + 1)
				{
					this.output.WriteLine("Invalid syntax. --plugin argument must be followed by the plugin path.", ConsoleColor.Red);
					this.log.LogWarning("Invalid syntax. --plugin argument must be followed by the plugin path.");
					return;
				}

				var pluginPath = args[indexOfPluginsArgument + 1]; 
				
				if (!File.Exists(pluginPath))
				{
					this.output.WriteLine($"The provided assembly <{pluginPath}> does not exists.", ConsoleColor.Red);
					this.log.LogWarning("The provided assembly <{PluginFolder}> does not exists.", pluginPath);
					return;
				}

				args.RemoveAt(indexOfPluginsArgument);
				args.RemoveAt(indexOfPluginsArgument);

				var assemblyFile = new FileInfo(pluginPath);
				if (!string.Equals(".dll", assemblyFile.Extension, StringComparison.OrdinalIgnoreCase))
				{
					this.output.WriteLine($"The provided file <{pluginPath}> is not a valid dll.", ConsoleColor.Red);
					this.log.LogWarning("The provided file <{PluginFolder}> is not a valid dll.", pluginPath);
					return;
				}
				
				
				var pluginName = assemblyFile.Name.Replace(".dll", string.Empty);

				this.log.LogDebug("Creating loader for plugin {PluginName}...",  pluginName);
				try
				{

					var loader = PluginLoader.CreateFromAssemblyFile(assemblyFile.FullName,
						config => config.PreferSharedTypes = true);

					loaders.Add((pluginName, loader, []));
				}
				catch (Exception ex)
				{
					this.output.WriteLine($"Error while creating loader for plugin {pluginName}: {ex.Message}", ConsoleColor.Red);
					log.LogError(ex, "Error while creating loader for plugin {PluginName}: {Message}", pluginName, ex.Message);
				}
			}










			foreach (var (pluginName, loader, otherAssemblies) in loaders)
			{
				this.log.LogDebug("Loading plugin {PluginName}...", pluginName);
				try
				{
					var pluginAssembly = loader.LoadDefaultAssembly();
					if (pluginAssembly == null)
					{
						log.LogWarning("Plugin {PluginName} does not contain a default assembly.", pluginName);
						continue;
					}
					foreach (var otherAssembly in otherAssemblies)
					{
						loader.LoadAssemblyFromPath(otherAssembly);
					}

					var moduleList = ScanForModules(pluginAssembly);
					var commandList = ScanForCommands(pluginAssembly, pluginName);
					var namespaceHelpers = ScanForNamespaceHelpers(pluginAssembly);

					this.moduleDefinitions.AddRange(moduleList);
					this.commandDefinitionList.AddRange(commandList);
					this.namespaceHelperList.AddRange(namespaceHelpers);

					CreateVerbTree();
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error while loading plugin {PluginName}: {Message}", pluginName, ex.Message);
				}
			}
		}


		/// <summary>
		/// Checks if the current plugin folder is marked for deletion.
		/// If yes, deletes the plugin folder
		/// </summary>
		/// <param name="pluginFolder"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static bool CheckDeletionMark(DirectoryInfo pluginFolder)
		{
			if (!pluginFolder.Exists)
				throw new ArgumentException("The specified plugin folder does not exists: " + pluginFolder.FullName, nameof(pluginFolder));

			var hasDeleteFile = pluginFolder.GetFiles(".delete").Length > 0;

			if (hasDeleteFile)
			{
				pluginFolder.Delete(true);
				pluginFolder.Refresh();
			}

			return hasDeleteFile;
		}




		public void InitializeFromAssembly(Assembly assembly)
		{
			var moduleList = ScanForModules(assembly);
			var commandList = ScanForCommands(assembly);
			var namespaceHelpers = ScanForNamespaceHelpers(assembly);

			this.moduleDefinitions.AddRange(moduleList);
			this.commandDefinitionList.AddRange(commandList);
			this.namespaceHelperList.AddRange(namespaceHelpers);

			CreateVerbTree();
		}








		private List<IModule> ScanForModules(Assembly assembly)
		{
			var moduleType = typeof(IModule);
			var moduleList = (from type in assembly.GetTypes()
							  where moduleType.IsAssignableFrom(type) && !type.IsAbstract && type.GetCustomAttribute<ObsoleteAttribute>() == null
							  let module = Activator.CreateInstance(type) as IModule
							  where module != null
							  select module).ToList();

			return moduleList;
		}






		private List<CommandDefinition> ScanForCommands(Assembly assembly, string? pluginName = null)
		{
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			var commandList = (from commandType in assembly.GetTypes()
							   let commandAttribute = commandType.GetCustomAttribute<CommandAttribute>()
							   where commandAttribute != null
							   where !commandType.IsAbstract && commandType.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
							   where !commandDefinitionList.Exists(c => c.CommandType == commandType)
							   let aliasAttributes = (commandType.GetCustomAttributes<AliasAttribute>()?.ToArray() ?? Array.Empty<AliasAttribute>())
							   let commandExecutorType = FindCommandExecutorType(commandType, assembly)
							   where commandExecutorType != null
							   select new CommandDefinition(commandAttribute, commandType, commandExecutorType, aliasAttributes, pluginName)).ToList();
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension

			foreach (var command in commandList)
			{
				foreach (var command2 in this.commandDefinitionList.Union(commandList))
				{
					if (command == command2) continue;

					if (command.TryMatch(command2, out var matchedAlias))
						throw new CommandException(CommandException.DuplicateCommand, $"Duplicate command {matchedAlias}.");
				}
			}

			return commandList;
		}

		private static Type? FindCommandExecutorType(Type commandType, Assembly assembly)
		{
			var executorType = typeof(ICommandExecutor<>).MakeGenericType(commandType);

			var commandExecutorType = (from type in assembly.GetTypes()
									   where executorType.IsAssignableFrom(type) && !type.IsAbstract
									   orderby type.FullName
									   select type).FirstOrDefault();

			return commandExecutorType;
		}

		private List<INamespaceHelper> ScanForNamespaceHelpers(Assembly assembly)
		{
			var helperType = typeof(INamespaceHelper);
			var namespaceHelpers = (from type in assembly.GetTypes()
									where helperType.IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
									let helper = Activator.CreateInstance(type) as INamespaceHelper
									where helper != null
									select helper).ToList();

			return namespaceHelpers;
		}

		public Type? GetExecutorTypeFor(Type commandType)
		{
			return this.commandDefinitionList
				.Where(x => x.CommandType == commandType)
				.Select(x => x.CommandExecutorType)
				.FirstOrDefault();
		}



		private void CreateVerbTree()
		{
			var list = new List<VerbNode>();

			foreach (var command in this.commandDefinitionList.OrderBy(x => x.ExpandedVerbs))
			{
				VerbNode? parent = null;
				var nodeList = list;
				for (var i = 0; i < command.Verbs.Count; i++)
				{
					var currentVerbs = command.Verbs.Take(i + 1).ToList();


					var node = nodeList.Find(x => x.Verb == command.Verbs[i]);
					if (node == null)
					{
						var helper = this.namespaceHelperList.Find(x => x.Verbs.SequenceEqual(currentVerbs, StringComparer.OrdinalIgnoreCase)) ?? NamespaceHelper.Empty;
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
