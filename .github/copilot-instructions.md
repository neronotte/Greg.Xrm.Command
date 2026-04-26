# Copilot Instructions for Greg.Xrm.Command (PACX)

PACX is a plugin-based CLI tool for Microsoft Dataverse / Power Platform, distributed as a .NET global tool (`pacx`). It supports ~100+ built-in commands plus dynamic plugin loading from `%appdata%/PACX/Plugins/`.

## Build & Test

```powershell
# Build
dotnet build
dotnet build --configuration Release

# Run all tests
dotnet test Greg.Xrm.Command.Core.TestSuite\Greg.Xrm.Command.Core.TestSuite.csproj

# Run a single test by full name
dotnet test Greg.Xrm.Command.Core.TestSuite\Greg.Xrm.Command.Core.TestSuite.csproj --filter "FullyQualifiedName=Greg.Xrm.Command.Commands.Auth.SelectCommandTest.ParseWithLongNameShouldWork"

# Run tests by category
dotnet test Greg.Xrm.Command.Core.TestSuite\Greg.Xrm.Command.Core.TestSuite.csproj --filter "TestCategory=Integration"

# Pack NuGet packages
dotnet pack Greg.Xrm.Command\Greg.Xrm.Command.csproj --configuration Release
dotnet pack Greg.Xrm.Command.Interfaces\Greg.Xrm.Command.Interfaces.csproj --configuration Release
```

## Project Layout

| Project                           | Role                                                       |
| --------------------------------- | ---------------------------------------------------------- |
| `Greg.Xrm.Command`                | CLI entry point — DI setup, bootstrapping, command runners |
| `Greg.Xrm.Command.Core`           | Core engine — all built-in commands, parsing, services     |
| `Greg.Xrm.Command.Interfaces`     | Public NuGet API — interfaces and attributes for plugins   |
| `Greg.Xrm.Command.Core.TestSuite` | MSTest unit tests using Moq                                |
| `sample/`                         | Reference plugin implementation                            |

## Architecture

### Command execution flow

```
Program.Main()
  → Bootstrapper.StartAsync()
      → CommandRegistry.InitializeFromAssembly()  // Core built-ins
      → CommandRegistry.ScanPluginsFolder()        // Dynamic plugins
  → CommandRunnerFactory → CommandRunnerCli | CommandRunnerInteractive
  → CommandParser.Parse(args) → Command object
  → CommandExecutorFactory → ICommandExecutor<T>.ExecuteAsync()
```

### Command pattern

Every command is a pair of classes in `Greg.Xrm.Command.Core\Commands\<Domain>\`:

```csharp
// Data/options holder — also handles validation
[Command("domain", "verb", HelpText = "...")]
public class VerbCommand : IValidatableObject, ICanProvideUsageExample
{
    [Option("option-name", "o", HelpText = "...", Order = 10)]
    [Required]
    public string? OptionName { get; set; }
}

// Business logic
public class VerbCommandExecutor : ICommandExecutor<VerbCommand>
{
    public async Task<CommandResult> ExecuteAsync(VerbCommand command, CancellationToken ct) { ... }
}
```

`CommandRegistry` discovers all `[Command]`-decorated classes via reflection — **no manual registration is needed**.

### Plugin system

Plugins are DLLs placed in `%appdata%/PACX/Plugins/`. They reference `Greg.Xrm.Command.Interfaces` and follow the same `[Command]` + `ICommandExecutor<T>` pattern. Plugins can also register an Autofac module for custom DI.

### Dependency injection

- `Program.cs` sets up `Microsoft.Extensions.DependencyInjection` + Autofac bridge (`AddAutofac()`)
- `Extensions.RegisterCommandExecutors()` auto-scans assemblies and registers all `ICommandExecutor<T>` implementations
- A child Autofac scope is created per command execution in `CommandExecutorFactory`

## Key Conventions

### Adding a new command

1. Create `Commands/<Domain>/VerbCommand.cs` with `[Command("domain", "verb")]`
2. Create `Commands/<Domain>/VerbCommandExecutor.cs` implementing `ICommandExecutor<VerbCommand>`
3. Add properties with `[Option]`; mark required ones with `[Required]`
4. Optionally implement `IValidatableObject.Validate()` for cross-option validation
5. Optionally implement `ICanProvideUsageExample.WriteUsageExamples()` for help text

### Attributes

| Attribute                      | Target   | Notes                                                             |
| ------------------------------ | -------- | ----------------------------------------------------------------- |
| `[Command(verb1, verb2, ...)]` | Class    | Multi-verb commands (e.g., `"auth", "select"`)                    |
| `[Option("long-name", "s")]`   | Property | `s` = single-char short name; `Order` controls help display order |
| `[Required]`                   | Property | Enforced by `CommandDefinition.CreateCommand()`                   |
| `[Alias("alt-verb")]`          | Class    | Alternative verb sequence for the same command                    |

### Option naming

- Long names use kebab-case: `--connection-name`
- Short names are single characters: `-n`
- `Order` (default 1000) controls display order in help — use lower values for more important options

### Test patterns

Tests live in `Greg.Xrm.Command.Core.TestSuite\Commands\<Domain>\`. Two common patterns:

```csharp
// Parser test — verifies CLI args map to correct command properties
[TestMethod]
public void ParseWithLongNameShouldWork()
{
    var command = Utility.TestParseCommand<SelectCommand>("auth", "select", "--name", "Conn1");
    Assert.AreEqual("Conn1", command.Name);
}

// Executor test — mocks services with Moq
[TestMethod]
public async Task ExecuteAsync_ShouldSucceed()
{
    var repoMock = new Mock<IOrganizationServiceRepository>();
    var outputMock = new Mock<IOutput>();
    var executor = new SelectCommandExecutor(repoMock.Object, outputMock.Object);
    var result = await executor.ExecuteAsync(new SelectCommand { Name = "Conn1" }, CancellationToken.None);
    Assert.IsTrue(result.IsSuccess);
}
```

Use `Utility.TestParseCommand<T>(verbs..., options...)` from the test project to parse commands in tests without spinning up the full CLI.

### CommandResult

Return `CommandResult.Success()` or `CommandResult.Fail("message")` from executors. `CommandResult` extends `Dictionary<string, object>` — populate it with output data for structured results.

### Output

Inject `IOutput` for all console writing. Never write directly to `Console`. `IOutput` supports color/style via Spectre.Console under the hood.

## Bug fixing rules

Follow these rules when fixing bugs:

1. If it's not already present, write a unit test that reproduces the bug. Run the test and assert the expected behavior. This ensures the bug is properly captured and prevents regressions.
2. Implement the fix in the production code to make the test pass. Ensure that the fix addresses the root cause of the bug and does not introduce new issues.
3. Refactor the code if necessary to improve readability, maintainability, or performance, while ensuring that all tests continue to pass. This step is optional and should be done with caution to avoid breaking existing functionality.
4. Run the full test suite to confirm that the fix does not cause any regressions or break existing functionality. This step is crucial to maintain the integrity of the codebase.
