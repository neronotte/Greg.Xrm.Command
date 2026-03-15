---
name: pacx-unit-test-writer
description: >
  Guide for writing local unit tests for PACX commands without relying on external services.
  Use this skill when asked to write, generate, or add unit tests for a PACX command class or executor.
  Tests produced by this skill are fully local — they never connect to Dataverse or any other external service.
---

# PACX Unit Test Writer

All tests live in `Greg.Xrm.Command.Core.TestSuite\Commands\<Domain>\` and mirror the source layout under `Greg.Xrm.Command.Core\Commands\<Domain>\`.

Every command needs **two** test files:

| File | Tests |
|---|---|
| `<Verb>CommandTest.cs` | CLI argument parsing — no DI, no Dataverse |
| `<Verb>CommandExecutorTest.cs` | Executor logic — all dependencies mocked |

`GlobalUsings.cs` already imports `Microsoft.VisualStudio.TestTools.UnitTesting`, `Greg.Xrm.Command.Services.Output`, and `Moq`. Do not add redundant `using` statements for those.

---

## Key test infrastructure

### `Utility.TestParseCommand<T>(params string[] args)`
Parses a CLI argument array into a typed command instance. Use this for all parser tests.

```csharp
var command = Utility.TestParseCommand<DeleteCommand>("table", "delete", "--name", "my_table");
```

### `OutputToMemory`
A concrete, in-memory `IOutput` implementation. Captures everything written to it.
- Use when you need to **assert on output text** (e.g., verify an error message was printed).
- `output.ToString()` returns the full captured text.
- Color-tagged output is serialised as `<Green>Done</Green>` — you can search for substrings.

```csharp
var output = new OutputToMemory();
// ... run executor ...
StringAssert.Contains(output.ToString(), "Done");
```

### `Mock<IOutput>`
Use instead of `OutputToMemory` when you need to **verify specific `IOutput` calls** with Moq's `Verify`.

```csharp
var mockOutput = new Mock<IOutput>();
mockOutput.Setup(o => o.Write(It.IsAny<object>())).Returns(mockOutput.Object);
mockOutput.Setup(o => o.WriteLine(It.IsAny<object>(), It.IsAny<ConsoleColor>())).Returns(mockOutput.Object);
// ...
mockOutput.Verify(o => o.WriteLine(
    It.Is<string>(s => s.Contains("Table not found")),
    It.IsAny<ConsoleColor>()), Times.Once);
```

> **Rule of thumb:** prefer `OutputToMemory` for most tests; switch to `Mock<IOutput>` only when you need to assert that a specific message was (or was not) written.

---

## File 1 — `<Verb>CommandTest.cs` (parser tests)

Test every `[Option]` with both its long name and its short name. Also assert that default values and `null` optionals are set correctly when options are omitted.

```csharp
namespace Greg.Xrm.Command.Commands.<Domain>
{
    [TestClass]
    public class <Verb>CommandTest
    {
        // ── Long-name parsing ──────────────────────────────────────────────────

        [TestMethod]
        public void ParseWithLongNameShouldWork()
        {
            var command = Utility.TestParseCommand<<Verb>Command>(
                "<noun>", "<verb>",
                "--name", "TestValue");

            Assert.AreEqual("TestValue", command.Name);
        }

        // ── Short-name parsing ─────────────────────────────────────────────────

        [TestMethod]
        public void ParseWithShortNameShouldWork()
        {
            var command = Utility.TestParseCommand<<Verb>Command>(
                "<noun>", "<verb>",
                "-n", "TestValue");

            Assert.AreEqual("TestValue", command.Name);
        }

        // ── Default values ─────────────────────────────────────────────────────
        // When options with DefaultValue are omitted, assert the expected default.
        // When optional options are omitted, assert null.

        [TestMethod]
        public void DefaultValuesShouldBeSetCorrectly()
        {
            var command = Utility.TestParseCommand<<Verb>Command>(
                "<noun>", "<verb>",
                "--name", "TestValue");

            Assert.AreEqual(ExpectedDefaultForSomeOption, command.SomeOption);
            Assert.IsNull(command.OptionalOption);
            Assert.IsNull(command.SolutionName);
        }

        // ── Enum options ───────────────────────────────────────────────────────

        [TestMethod]
        public void EnumOptionShouldBeParsedCorrectly()
        {
            var command = Utility.TestParseCommand<<Verb>Command>(
                "<noun>", "<verb>",
                "--name", "TestValue",
                "--ownership", "Organization");

            Assert.AreEqual(OwnershipTypes.OrgOwned, command.Ownership);
        }

        // ── Boolean flags ──────────────────────────────────────────────────────

        [TestMethod]
        public void BoolFlagShouldDefaultToFalse()
        {
            var command = Utility.TestParseCommand<<Verb>Command>("<noun>", "<verb>", "--name", "v");
            Assert.IsFalse(command.SomeFlag);
        }

        [TestMethod]
        public void BoolFlagShouldBeParsedWhenProvided()
        {
            var command = Utility.TestParseCommand<<Verb>Command>(
                "<noun>", "<verb>",
                "--name", "v",
                "--someFlag");

            Assert.IsTrue(command.SomeFlag);
        }

        // ── Parameterised (use [DataRow] for multiple input variants) ──────────

        [TestMethod]
        [DataRow("--name",      "v1")]
        [DataRow("--otherName", "v2")]
        public void AllNameAliasesShouldMapToSameProperty(string optionKey, string optionValue)
        {
            var command = Utility.TestParseCommand<<Verb>Command>("<noun>", "<verb>", optionKey, optionValue);
            Assert.IsNotNull(command.Name);
        }
    }
}
```

---

## File 2 — `<Verb>CommandExecutorTest.cs` (executor unit tests)

### Boilerplate — standard Dataverse mock setup

```csharp
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.<Domain>
{
    [TestClass]
    public class <Verb>CommandExecutorTest
    {
        // Helper: builds mocks wired together the same way as the real DI chain.
        private static (
            OutputToMemory output,
            Mock<IOrganizationServiceRepository> repoMock,
            Mock<IOrganizationServiceAsync2> crmMock)
        CreateMocks()
        {
            var output = new OutputToMemory();
            var crmMock = new Mock<IOrganizationServiceAsync2>();
            var repoMock = new Mock<IOrganizationServiceRepository>();

            repoMock
                .Setup(r => r.GetCurrentConnectionAsync())
                .ReturnsAsync(crmMock.Object);

            return (output, repoMock, crmMock);
        }
    }
}
```

### Happy-path test

```csharp
[TestMethod]
public async Task ExecuteAsync_ShouldSucceed_WhenInputIsValid()
{
    // Arrange
    var (output, repoMock, crmMock) = CreateMocks();
    OrganizationRequest? capturedRequest = null;

    crmMock
        .Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
        .Callback<OrganizationRequest>(r => capturedRequest = r)
        .ReturnsAsync(new <ExpectedResponseType>()); // e.g. DeleteEntityResponse

    var executor = new <Verb>CommandExecutor(output, repoMock.Object);

    // Act
    var result = await executor.ExecuteAsync(
        new <Verb>Command { Name = "some_value" },
        CancellationToken.None);

    // Assert — result
    Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

    // Assert — Dataverse was called
    repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Once);
    crmMock.Verify(c => c.ExecuteAsync(It.IsAny<<ExpectedRequestType>>()), Times.Once);

    // Assert — request content
    Assert.IsNotNull(capturedRequest);
    var typedRequest = capturedRequest as <ExpectedRequestType>;
    Assert.IsNotNull(typedRequest);
    Assert.AreEqual("some_value", typedRequest./* relevant property */);
}
```

### Failure — Dataverse returns an error

```csharp
[TestMethod]
public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
{
    // Arrange
    var (output, repoMock, crmMock) = CreateMocks();

    crmMock
        .Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
        .ThrowsAsync(new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
            new Microsoft.Xrm.Sdk.OrganizationServiceFault(),
            "Simulated Dataverse fault"));

    var executor = new <Verb>CommandExecutor(output, repoMock.Object);

    // Act
    var result = await executor.ExecuteAsync(
        new <Verb>Command { Name = "some_value" },
        CancellationToken.None);

    // Assert
    Assert.IsFalse(result.IsSuccess);
    Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
}
```

### Failure — prerequisite check fails (e.g. solution not found, entity not found)

```csharp
[TestMethod]
public async Task ExecuteAsync_ShouldFail_WhenSolutionDoesNotExist()
{
    // Arrange
    var (output, repoMock, crmMock) = CreateMocks();

    // Simulate empty result set for the pre-flight query
    crmMock
        .Setup(c => c.RetrieveMultipleAsync(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryBase>()))
        .ReturnsAsync(new Microsoft.Xrm.Sdk.EntityCollection()); // empty

    var executor = new <Verb>CommandExecutor(output, repoMock.Object);

    // Act
    var result = await executor.ExecuteAsync(
        new <Verb>Command { Name = "v", SolutionName = "nonexistent" },
        CancellationToken.None);

    // Assert
    Assert.IsFalse(result.IsSuccess);
    // The main operation must NOT have been called
    crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
}
```

### Failure — no default solution set

```csharp
[TestMethod]
public async Task ExecuteAsync_ShouldFail_WhenNoSolutionProvidedAndNoDefault()
{
    // Arrange
    var (output, repoMock, crmMock) = CreateMocks();

    repoMock
        .Setup(r => r.GetCurrentDefaultSolutionAsync())
        .ReturnsAsync((string?)null);

    var executor = new <Verb>CommandExecutor(output, repoMock.Object);

    // Act
    var result = await executor.ExecuteAsync(
        new <Verb>Command { Name = "some_value" /* SolutionName intentionally omitted */ },
        CancellationToken.None);

    // Assert
    Assert.IsFalse(result.IsSuccess);
    crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
}
```

### Mocking additional injected services

When the executor depends on more services beyond `IOrganizationServiceRepository`, mock and inject them the same way:

```csharp
var mockPluralization = new Mock<IPluralizationFactory>();
mockPluralization
    .Setup(p => p.CreateFor(It.IsAny<int>()))
    .Returns(Mock.Of<IPluralizationStrategy>(s =>
        s.GetPluralForAsync(It.IsAny<string>()) == Task.FromResult("Values")));

var executor = new <Verb>CommandExecutor(output, repoMock.Object, mockPluralization.Object);
```

### Parameterised executor tests with `[DataRow]`

Use `[DataRow]` when several input combinations should all produce the same outcome:

```csharp
[TestMethod]
[DataRow("value_a", true)]
[DataRow("value_b", true)]
[DataRow(null,      false)]
public async Task ExecuteAsync_ShouldReturnExpectedResult(string? inputName, bool expectSuccess)
{
    var (output, repoMock, crmMock) = CreateMocks();
    crmMock
        .Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
        .ReturnsAsync(new OrganizationResponse());

    var executor = new <Verb>CommandExecutor(output, repoMock.Object);

    var result = await executor.ExecuteAsync(
        new <Verb>Command { Name = inputName },
        CancellationToken.None);

    Assert.AreEqual(expectSuccess, result.IsSuccess);
}
```

---

## Testing `IValidatableObject.Validate()` directly

When a command implements `IValidatableObject`, test `Validate()` in isolation — no parsing, no executor.

```csharp
[TestMethod]
public void Validate_ShouldFail_WhenMutuallyExclusiveOptionsAreProvided()
{
    var command = new <Verb>Command
    {
        OptionA = "value",
        OptionB = "also-value" // these two cannot coexist
    };

    var context = new System.ComponentModel.DataAnnotations.ValidationContext(command);
    var results = command.Validate(context).ToList();

    Assert.AreEqual(1, results.Count);
    CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(<Verb>Command.OptionA));
}

[TestMethod]
public void Validate_ShouldPass_WhenOnlyOneOptionIsProvided()
{
    var command = new <Verb>Command { OptionA = "value" };
    var context = new System.ComponentModel.DataAnnotations.ValidationContext(command);
    var results = command.Validate(context).ToList();
    Assert.AreEqual(0, results.Count);
}
```

---

## What NOT to do

| ❌ Avoid | ✅ Do instead |
|---|---|
| `new OrganizationServiceRepository(...)` in a unit test | `new Mock<IOrganizationServiceRepository>()` |
| `[TestCategory("Integration")]` on a local test | Omit the category entirely |
| `task.Wait()` | `await executor.ExecuteAsync(...)` |
| Asserting only `Assert.IsNotNull(output.ToString())` | Assert on `result.IsSuccess`, mock `.Verify()`, and request content |
| Real file I/O or environment variables | Mock the service that reads them |
| `new OutputToConsole()` | `new OutputToMemory()` or `new Mock<IOutput>()` |

---

## Checklist before finishing

- [ ] `<Verb>CommandTest.cs` covers every `[Option]` with long name and short name
- [ ] Default values and `null` optionals are asserted when options are omitted
- [ ] Boolean flags tested both absent (default `false`) and present (`true`)
- [ ] `[DataRow]` used for options that have multiple meaningful input variants
- [ ] `<Verb>CommandExecutorTest.cs` has a happy-path test
- [ ] Happy-path asserts `result.IsSuccess`, verifies Dataverse was called, and inspects the captured request
- [ ] At least one failure test covers a Dataverse fault (`FaultException<OrganizationServiceFault>`)
- [ ] Pre-flight guard failures tested (solution not found, entity not found, etc.) and assert Dataverse operation was **not** called
- [ ] No-default-solution path tested if the command reads `GetCurrentDefaultSolutionAsync()`
- [ ] `IValidatableObject.Validate()` tested directly if the command implements it
- [ ] No test uses a real `OrganizationServiceRepository` or connects to Dataverse
- [ ] No test is marked `[TestCategory("Integration")]`
- [ ] `dotnet test Greg.Xrm.Command.Core.TestSuite\Greg.Xrm.Command.Core.TestSuite.csproj` passes
