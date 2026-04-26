using Greg.Xrm.Command.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greg.Xrm.Command.Parsing
{
	[TestClass]
	public class CommandRegistryCollisionTest
	{
		private static CommandRegistry CreateRegistry(out OutputToMemory output)
		{
			var log = NullLogger<CommandRegistry>.Instance;
			output = new OutputToMemory();
			var storage = new Storage();
			return new CommandRegistry(log, output, storage);
		}

		[TestMethod]
		public void ScanForCommands_ShouldRegisterCommand_WhenItDeclaresEnvironmentLongName()
		{
			var registry = CreateRegistry(out _);

			registry.InitializeFromAssembly(typeof(StubConflictingLongNameCommand).Assembly);

			var found = registry.Commands.Any(c => c.CommandType == typeof(StubConflictingLongNameCommand));
			Assert.IsTrue(found, "Command with --environment long name should be registered (no longer skipped).");
		}

		[TestMethod]
		public void ScanForCommands_ShouldRegisterCommand_WhenItDeclaresEnvironmentShortName()
		{
			var registry = CreateRegistry(out _);

			registry.InitializeFromAssembly(typeof(StubConflictingShortNameCommand).Assembly);

			var found = registry.Commands.Any(c => c.CommandType == typeof(StubConflictingShortNameCommand));
			Assert.IsTrue(found, "Command with -env short name should be registered (no longer skipped).");
		}

		[TestMethod]
		public void ScanForCommands_ShouldNotEmitWarning_WhenCommandDeclaresEnvironment()
		{
			var registry = CreateRegistry(out var output);

			registry.InitializeFromAssembly(typeof(StubConflictingLongNameCommand).Assembly);

			var outputText = output.ToString();
			Assert.IsFalse(outputText.Contains("reserved option"), "No warning should be emitted for --environment declarations.");
		}

		[TestMethod]
		public void ScanForCommands_ShouldNotSkipCommand_WhenNoConflict()
		{
			var registry = CreateRegistry(out _);

			registry.InitializeFromAssembly(typeof(StubNoConflictCommand).Assembly);

			var found = registry.Commands.Any(c => c.CommandType == typeof(StubNoConflictCommand));
			Assert.IsTrue(found, "Command with no conflicting options should be registered.");
		}
	}


	// ── Stub commands (defined here so they live in the test assembly) ────────

	[Command("stub", "conflicting-env-long", HelpText = "Stub with --environment long name")]
	internal class StubConflictingLongNameCommand
	{
		[Option("environment", "e", HelpText = "Environment option")]
		public string? Environment { get; set; }
	}

	internal class StubConflictingLongNameCommandExecutor : ICommandExecutor<StubConflictingLongNameCommand>
	{
		public Task<CommandResult> ExecuteAsync(StubConflictingLongNameCommand command, CancellationToken ct)
			=> Task.FromResult(CommandResult.Success());
	}

	[Command("stub", "conflicting-env-short", HelpText = "Stub with -env short name")]
	internal class StubConflictingShortNameCommand
	{
		[Option("other-env", "env", HelpText = "Option with -env short name")]
		public string? OtherEnv { get; set; }
	}

	internal class StubConflictingShortNameCommandExecutor : ICommandExecutor<StubConflictingShortNameCommand>
	{
		public Task<CommandResult> ExecuteAsync(StubConflictingShortNameCommand command, CancellationToken ct)
			=> Task.FromResult(CommandResult.Success());
	}

	[Command("stub", "noconflict", HelpText = "Stub with no conflicting options")]
	internal class StubNoConflictCommand
	{
		[Option("name", "n", HelpText = "A normal option")]
		public string? Name { get; set; }
	}

	internal class StubNoConflictCommandExecutor : ICommandExecutor<StubNoConflictCommand>
	{
		public Task<CommandResult> ExecuteAsync(StubNoConflictCommand command, CancellationToken ct)
			=> Task.FromResult(CommandResult.Success());
	}
}
