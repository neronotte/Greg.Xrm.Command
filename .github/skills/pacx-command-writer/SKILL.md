---
name: pacx-command-writer
description: >
    Guide for writing new PACX CLI commands (verbs) following established project conventions.
    Use this skill when asked to create, scaffold, or add a new command to the Greg.Xrm.Command repository.
---

# PACX Command Writer

Every PACX command is a pair of files in `Greg.Xrm.Command.Core\Commands\<Domain>\`. There is no manual DI registration — `Extensions.RegisterCommandExecutors()` auto-scans all `ICommandExecutor<T>` implementations at startup.

## Rules

- Plan in advance — think through the command's options, validation, and usage examples before implementation.
- Draft an implementation plan in markdown in the /docs folder before coding.
    - The implementation plan must contain the list of steps required to implement the command.
    - When you start working on a single step, update the step title putting a 🕒 before the step title
    - When a step is completed, update the step title putting a ✅ before the step title
    - Implementation plan must include also unit tests for all implemented steps.
- Commit after each implementation step.
- Before pushing, run a code review of the changes to ensure adherence to project conventions and correctness.
- If in the code you need to use dataverse tables, create a model class in /Greg.Xrm.Command.Core/Model implementing `EntityWrapper` and a repository to manage the queries. Use `Greg.Xrm.Command.Core\Model\WebResource.cs` as a reference sample.

---

## File 1 — `<Verb>Command.cs` (options holder)

This class declares CLI options via attributes and optionally handles cross-option validation and usage examples. It must **not** contain business logic.

```csharp
using Greg.Xrm.Command.Parsing;             // ICanProvideUsageExample, MarkdownWriter
using Greg.Xrm.Command.Services;            // MarkdownWriter (via Parsing namespace)
using System.ComponentModel.DataAnnotations; // [Required], IValidatableObject

namespace Greg.Xrm.Command.Commands.<Domain>
{
    [Command("<noun>", "<verb>", HelpText = "One-sentence description.")]
    [Alias("<verb>", "<noun>")]   // only add when reversed order is natural
    public class <Verb>Command : IValidatableObject, ICanProvideUsageExample
    //           ─────────────────────────────────   ──────────────────────
    //           implement only when cross-option     implement for non-trivial
    //           constraints exist                    commands to aid discoverability
    {
        // Required options first — low Order values (1, 2, 3…)
        [Option("name", "n", Order = 1, HelpText = "...")]
        [Required]
        public string? Name { get; set; }

        // Optional options — grouped by concern, higher Order values
        [Option("description", "d", Order = 10, HelpText = "...")]
        public string? Description { get; set; }

        // Solution is always last (Order = 50+)
        [Option("solution", "s", Order = 50, HelpText = "Unmanaged solution name. Uses the current default solution if omitted.")]
        public string? SolutionName { get; set; }

        // Cross-option validation only — single-option guards go in the executor
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (/* mutually exclusive condition */)
                yield return new ValidationResult("...", new[] { nameof(OptionA), nameof(OptionB) });
        }

        public void WriteUsageExamples(MarkdownWriter writer)
        {
            writer.WriteParagraph("Minimal usage:");
            writer.WriteCodeBlock("pacx <noun> <verb> --name \"value\"", "Powershell");
        }
    }
}
```

### `[Option]` conventions

| Property       | Rule                                                                        |
| -------------- | --------------------------------------------------------------------------- |
| `longName`     | **camelCase** — e.g. `schemaName`, `displayName`, `requiredLevel`           |
| `shortName`    | Short abbreviation, unique within the command — e.g. `"n"`, `"sn"`, `"par"` |
| `HelpText`     | Always set — shown in `pacx help` and interactive mode                      |
| `Order`        | Required options: 1–9; grouped optional options: 10–49; `--solution`: 50+   |
| `DefaultValue` | Set on the attribute when there is a meaningful default the user should see |
| `[Required]`   | Mark all mandatory options                                                  |

### `[Command]` and `[Alias]` conventions

- Verb order in `[Command]` is always **noun first**: `[Command("table", "create", ...)]`.
- `[Alias]` reverses the order when natural: `[Alias("create", "table")]`.
- Only add `[Alias]` when the reversed form is genuinely useful.

---

## File 2 — `<Verb>CommandExecutor.cs` (business logic)

```csharp
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel; // FaultException<OrganizationServiceFault>

namespace Greg.Xrm.Command.Commands.<Domain>
{
    public class <Verb>CommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<<Verb>Command>
    {
        public async Task<CommandResult> ExecuteAsync(<Verb>Command command, CancellationToken cancellationToken)
        {
            // Step 1 — always connect first
            output.Write("Connecting to the current dataverse environment...");
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            output.WriteLine("Done", ConsoleColor.Green);

            try
            {
                // Step 2 — resolve default solution when the option is omitted
                var solutionName = command.SolutionName;
                if (string.IsNullOrWhiteSpace(solutionName))
                {
                    solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
                    if (solutionName == null)
                        return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
                }

                // Step 3 — report each logical step inline
                output.Write("Performing the operation...");
                var request = new OrganizationRequest(/* ... */);
                var response = await crm.ExecuteAsync(request);
                output.WriteLine(" Done", ConsoleColor.Green);

                // Step 4 — return success with key output values
                var result = CommandResult.Success();
                result["EntityId"] = response./* ... */;
                return result;
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                return CommandResult.Fail(ex.Message, ex);
            }
        }

        // Non-trivial computations go in private static helpers to keep ExecuteAsync readable
        private static string ComputeSchemaName(string displayName, string publisherPrefix)
        {
            // ...
        }
    }
}
```

### Executor conventions

- Command infrastructure (`CommandRunnerBase`) validates DataAnnotations and `IValidatableObject` before calling an executor. Do not call `Validator.TryValidateObject` again in executors or duplicate command validation there. Declare input constraints on the command; keep only business checks requiring resolved data (for example whether a privilege supports a depth) in the executor. Tests for command validation belong in the command test file, not in tests that invoke the executor directly.
- **Never** write to `Console` directly — always use `IOutput`.
- If you need additional output formatting you can use `IAnsiConsole` from `Greg.Xrm.Command.Services.Output`, which wraps `Spectre.Console` functionality.
- Inline progress pattern: `output.Write("Step…")` → do async work → `output.WriteLine(" Done", ConsoleColor.Green)`.
- Highlight important user-provided values in yellow: `output.Write(command.SchemaName, ConsoleColor.Yellow)`.
- Resolve omitted `--solution` via `organizationServiceRepository.GetCurrentDefaultSolutionAsync()`.
- Catch `FaultException<OrganizationServiceFault>` at minimum. Catch `Exception` when non-Dataverse failures are possible.
- Put non-trivial computation in `private static` helper methods.
- Populate `CommandResult` with key/value output pairs for structured consumers.
- `CommandResult` output values must be scalars (strings, numbers, booleans, GUIDs), never arrays or collection objects: the CLI renders them via `ToString()` and displays .NET type names instead of their contents. Render detailed collections through `IOutput.WriteTable` and return counts or explicitly formatted summary strings. Keep collection-based view models internal; tests can inspect service results or capture table rows, rather than requiring collections in command output parameters.

---

## Tests

Once the command and executor files are in place, use the **`pacx-unit-test-writer`** skill to generate the corresponding test files.

---

## Checklist before finishing

- [ ] `<Verb>Command.cs` placed in `Greg.Xrm.Command.Core\Commands\<Domain>\`
- [ ] `[Command]` verbs are noun-first: `"<noun>", "<verb>"`
- [ ] `[Alias]` added only when the reversed order is useful
- [ ] All `[Option]` have `HelpText`, sensible `Order`, `DefaultValue` where applicable
- [ ] Required options marked with `[Required]`
- [ ] `IValidatableObject.Validate()` implemented only for cross-option constraints
- [ ] `ICanProvideUsageExample.WriteUsageExamples()` implemented for non-trivial commands
- [ ] `<Verb>CommandExecutor.cs` placed in the same domain folder
- [ ] Executor uses `IOutput` for all console output (never `Console.Write*`)
- [ ] Progress pattern (`Write(…)` / `WriteLine(" Done", Green)`) used for each step
- [ ] `FaultException<OrganizationServiceFault>` caught and mapped to `CommandResult.Fail`
- [ ] `CommandResult` populated with key output values
- [ ] `CommandResult` contains only scalar output values, no arrays or collections
- [ ] Tests written using the **`pacx-unit-test-writer`** skill
- [ ] `dotnet build` passes
- [ ] `dotnet test Greg.Xrm.Command.Core.TestSuite\Greg.Xrm.Command.Core.TestSuite.csproj` passes
