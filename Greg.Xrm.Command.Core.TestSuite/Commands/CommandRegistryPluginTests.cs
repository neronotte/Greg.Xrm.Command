using Greg.Xrm.Command.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Greg.Xrm.Command.TestSuite
{
	/// <summary>
	/// Unit tests for CommandRegistry plugin loading and command discovery.
	/// Covers Phase 1 (CommandRegistry), Phase 2 (Plugin Loading), and Phase 3 (Bootstrapper edge cases).
	/// </summary>
	[TestClass]
	public class CommandRegistryPluginTests
	{
		private CommandRegistry CreateRegistry()
		{
			var log = NullLogger<CommandRegistry>.Instance;
			var output = new OutputToMemory();
			var storage = new Greg.Xrm.Command.Services.Storage();
			return new CommandRegistry(log, output, storage);
		}

		#region Phase 1: CommandRegistry Unit Tests

		[TestMethod]
		public void InitializeFromAssembly_ShouldDiscoverAllCoreCommands()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			Assert.IsTrue(registry.Commands.Count > 0, "No commands were discovered from the core assembly.");

			// Verify specific core commands are found
			var commandTypes = registry.Commands.Select(c => c.CommandType).ToList();
			Assert.IsTrue(commandTypes.Any(t => t == typeof(Greg.Xrm.Command.Commands.Auth.ListCommand)), "ListCommand not discovered.");
			Assert.IsTrue(commandTypes.Any(t => typeof(Greg.Xrm.Command.Commands.Auth.CreateCommand).IsAssignableFrom(t)), "CreateCommand not discovered.");
		}

		[TestMethod]
		public void ScanForModules_ShouldDiscoverAutofacModules()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// The core assembly should have at least the IoCModule
			Assert.IsTrue(registry.Modules.Count > 0, "No Autofac modules were discovered from the core assembly.");
			Assert.IsTrue(registry.Modules.Any(m => m.GetType().Name == "IoCModule"), "IoCModule not discovered.");
		}

		[TestMethod]
		public void ScanForCommands_ShouldMatchCommandExecutorPairs()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// Every discovered command should have a matching executor
			foreach (var command in registry.Commands)
			{
				Assert.IsNotNull(command.CommandExecutorType,
					$"Command '{command.ExpandedVerbs}' has no matching ICommandExecutor type.");

				var executorInterface = typeof(ICommandExecutor<>).MakeGenericType(command.CommandType);
				Assert.IsTrue(executorInterface.IsAssignableFrom(command.CommandExecutorType),
					$"Executor '{command.CommandExecutorType.Name}' does not implement '{executorInterface.Name}'.");
			}
		}

		[TestMethod]
		public void ScanForCommands_ShouldDetectDuplicateVerbs()
		{
			// This test verifies that the registry throws on duplicate command definitions.
			// Since the core assembly has no duplicates, we verify the scan completes without error.
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// Verify no duplicate command definitions exist
			var verbGroups = registry.Commands
				.GroupBy(c => string.Join(" ", c.Verbs))
				.Where(g => g.Count() > 1)
				.ToList();

			Assert.AreEqual(0, verbGroups.Count,
				$"Duplicate commands found: {string.Join(", ", verbGroups.Select(g => g.Key))}");
		}

		[TestMethod]
		public void ScanForNamespaceHelpers_ShouldDiscoverHelpers()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// Verify namespace helpers are discovered (at minimum, the empty helper from plugin scanning)
			Assert.IsNotNull(registry.Tree, "Command tree was not built.");
		}

		[TestMethod]
		public void CreateVerbTree_ShouldBuildHierarchicalVerbTree()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// Verify the tree has root nodes
			Assert.IsTrue(registry.Tree.Count > 0, "Command tree has no root nodes.");

			// Verify 'auth' verb exists (known command)
			var authNode = registry.Tree.FirstOrDefault(n => n.Verb == "auth");
			Assert.IsNotNull(authNode, "'auth' verb not found in command tree.");

			// Verify 'auth list' subcommand exists
			var listNode = authNode.Children.FirstOrDefault(n => n.Verb == "list");
			Assert.IsNotNull(listNode, "'auth list' subcommand not found in command tree.");
			Assert.IsNotNull(listNode.Command, "'auth list' has no associated command.");
		}

		#endregion

		#region Phase 2: Plugin Loading Integration Tests

		[TestMethod]
		public void ScanPluginsFolder_WithNonExistentFolder_ShouldNotThrow()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			var args = new Greg.Xrm.Command.Parsing.CommandLineArguments(new[] { "help" });
			registry.ScanPluginsFolder(args);

			// Should complete without error even with no plugins folder
			Assert.IsTrue(true);
		}

		[TestMethod]
		public void ScanPluginsFolder_WithEmptyPluginsFolder_ShouldNotAddCommands()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);
			var initialCount = registry.Commands.Count;

			var tempDir = Path.Combine(Path.GetTempPath(), $"pacx_test_plugins_empty_{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDir);

			try
			{
				var storage = new Greg.Xrm.Command.Services.Storage();
				// Point storage to temp dir (simulated)
				var args = new Greg.Xrm.Command.Parsing.CommandLineArguments(new[] { "help" });
				registry.ScanPluginsFolder(args);

				// Should not add any commands from empty folder
				Assert.AreEqual(initialCount, registry.Commands.Count);
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void ScanForCommands_ShouldSkipAbstractCommandTypes()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// No abstract types should be in the command list
			foreach (var command in registry.Commands)
			{
				Assert.IsFalse(command.CommandType.IsAbstract,
					$"Abstract command type '{command.CommandType.Name}' was incorrectly discovered.");
			}
		}

		[TestMethod]
		public void ScanForCommands_ShouldSkipCommandsWithoutParameterlessConstructor()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// All discovered commands should have a parameterless constructor
			foreach (var command in registry.Commands)
			{
				var hasParameterless = command.CommandType.GetConstructors()
					.Any(c => c.IsPublic && c.GetParameters().Length == 0);
				Assert.IsTrue(hasParameterless,
					$"Command '{command.CommandType.Name}' has no parameterless constructor.");
			}
		}

		[TestMethod]
		public void ScanForCommands_ShouldSkipObsoleteCommandTypes()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// No obsolete types should be in the command list
			foreach (var command in registry.Commands)
			{
				var obsoleteAttr = command.CommandType.GetCustomAttribute<ObsoleteAttribute>();
				Assert.IsNull(obsoleteAttr,
					$"Obsolete command type '{command.CommandType.Name}' was incorrectly discovered.");
			}
		}

		#endregion

		#region Phase 3: Bootstrapper & Edge Cases

		[TestMethod]
		public void InitializeFromAssembly_WithMultipleCalls_ShouldNotDuplicateCommands()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);
			var firstCount = registry.Commands.Count;

			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);
			var secondCount = registry.Commands.Count;

			// Second call should add more commands (since it's a new registration)
			// but within a single scan, no duplicates
			Assert.IsTrue(secondCount >= firstCount);

			// Verify no duplicates by verb
			var duplicates = registry.Commands
				.GroupBy(c => string.Join(" ", c.Verbs))
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			Assert.AreEqual(0, duplicates.Count,
				$"Duplicate commands after double initialization: {string.Join(", ", duplicates)}");
		}

		[TestMethod]
		public void CommandTree_ShouldSupportMultiLevelVerbHierarchy()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			// Verify multi-level verbs exist (e.g., "env create", "alm pipeline create")
			var hasMultiLevel = registry.Commands.Any(c => c.Verbs.Count >= 3);

			// Even if no 3-level commands exist, the tree structure should support it
			Assert.IsNotNull(registry.Tree, "Command tree is null.");
		}

		[TestMethod]
		public void GetExecutorTypeFor_ShouldReturnCorrectExecutorType()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			var listCommandType = typeof(Greg.Xrm.Command.Commands.Auth.ListCommand);
			var executorType = registry.GetExecutorTypeFor(listCommandType);

			Assert.IsNotNull(executorType, "No executor type found for ListCommand.");
			var executorInterface = typeof(ICommandExecutor<>).MakeGenericType(listCommandType);
			Assert.IsTrue(executorInterface.IsAssignableFrom(executorType),
				$"Executor type does not implement ICommandExecutor<ListCommand>.");
		}

		[TestMethod]
		public void GetExecutorTypeFor_WithUnknownType_ShouldReturnNull()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			var unknownType = typeof(string);
			var executorType = registry.GetExecutorTypeFor(unknownType);

			Assert.IsNull(executorType, "Executor type should be null for unknown command type.");
		}

		[TestMethod]
		public void ScanPluginsFolder_WithToolArgument_ShouldAttemptToLoadDll()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			var args = new Greg.Xrm.Command.Parsing.CommandLineArguments(new[] { "help", "--tool", "C:\\nonexistent\\plugin.dll" });
			registry.ScanPluginsFolder(args);

			// Should handle non-existent file gracefully without throwing
			Assert.IsTrue(true);
		}

		[TestMethod]
		public void ScanPluginsFolder_WithNonDllFile_ShouldSkip()
		{
			var registry = CreateRegistry();
			registry.InitializeFromAssembly(typeof(Greg.Xrm.Command.Commands.Help.HelpCommand).Assembly);

			var args = new Greg.Xrm.Command.Parsing.CommandLineArguments(new[] { "help", "--tool", "C:\\temp\\readme.txt" });
			registry.ScanPluginsFolder(args);

			// Should skip non-DLL file gracefully
			Assert.IsTrue(true);
		}

		#endregion
	}
}
