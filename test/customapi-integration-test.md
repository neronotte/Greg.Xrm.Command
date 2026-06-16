# Building and Testing Dataverse Custom APIs with PACX CLI

**by Riccardo Gregori — Power Platform Solution Architect & Microsoft MVP**

---

## Introduction

Dataverse Custom APIs are one of the most powerful extensibility points in the Power Platform. They let you define typed, self-documenting messages that can be called from anywhere — Power Automate, Canvas Apps, PCF controls, external services — with full SDK support and plugin-driven logic.

The problem has always been _tooling friction_: creating a Custom API traditionally requires navigating the Maker Portal's Custom API form, manually creating request parameters and response properties one by one, then separately registering a plugin, binding it, and hoping you got the uniquename casing right when writing the plugin code.

This post documents the full lifecycle of a Custom API — from creation to live execution — using the new `customapi` command suite in [PACX](https://github.com/neronotte/Greg.Xrm.Command). Every command below was run against a real environment during development of the feature itself, so what you're reading is a live integration test journal.

---

## The Scenario

We'll implement `nn_PacxSum`: a simple Custom API that takes two integers (`Addend1`, `Addend2`) and returns their sum (`Result`). Trivial logic, but it exercises every part of the stack.

Environment details:

- **Environment:** `rg01` (`https://rg01.crm4.dynamics.com`)
- **Solution:** `master`
- **Publisher prefix:** `nn`
- **Tool:** PACX local build from `feature/customapi-commands` branch

---

## Step 1 — Create the Custom API

```powershell
pacx customapi create `
  -d "Pacx Sum" `
  --description "A simple integer sum custom API: given two integers, returns their sum. Used as PACX integration test." `
  -p "Addend1:Integer, Addend2:Integer" `
  -r "Result:Integer"
```

Output:

```
Connecting to the current dataverse environment...Done
Validating solution 'master'...Done
Checking if Custom API 'nn_PacxSum' already exists...Not found
Creating Custom API 'nn_PacxSum'...Done
  Warning: could not add component to solution 'master': ...
  Adding param 'Addend1'...Done
  Adding param 'Addend2'...Done
  Adding response 'Result'...Done

Custom API created: nn_PacxSum
Display name:       Pacx Sum
Solution:           master
Parameters:   Addend1 (Integer), Addend2 (Integer)
Responses:    Result (Integer)
```

A single command creates the `customapi`, both `customapirequestparameter` records, and the `customapiresponseproperty` record. PACX infers the unique name automatically from the display name and the solution's publisher prefix: `"Pacx Sum"` → `nn_PacxSum`.

### Naming conventions enforced by PACX

| Entity            | `name` (display) | `uniquename` (SDK key)  |
| ----------------- | ---------------- | ----------------------- |
| Custom API        | `Pacx Sum`       | `nn_PacxSum`            |
| Request parameter | `Addend1`        | `nn_PacxSum-in-Addend1` |
| Response property | `Result`         | `nn_PacxSum-out-Result` |

The `uniquename` of request parameters and response properties is what Dataverse uses as the key in `OrganizationRequest.Parameters` and `OrganizationResponse.Results` respectively. The `{apiname}-in-{param}` / `{apiname}-out-{prop}` pattern prevents name collisions across APIs while remaining readable in plugin code.

---

## Step 2 — Inspect with `customapi list`

```powershell
pacx customapi list
```

Returns a table of all Custom APIs in the environment with their unique name, display name, type, binding, and plugin status.

---

## Step 3 — Describe the API

```powershell
pacx customapi describe -n nn_PacxSum
```

Output:

```
Custom API:
  Unique Name:  nn_PacxSum
  Display Name: Pacx Sum
  Type:         Action (POST)
  Binding:      Global
  Private:      No
  Step Types:   Sync and Async
  Privilege:    (none)
  Plugin:       (unbound)
  Description:  A simple integer sum custom API: given two integers, returns their sum.

  Signature:    nn_PacxSum(Addend1: Integer, Addend2: Integer) -> Result: Integer

Request Parameters:
+---------+---------+----------+-------------+
| Name    | Type    | Required | Description |
+---------+---------+----------+-------------+
| Addend1 | Integer | Yes      |             |
| Addend2 | Integer | Yes      |             |
+---------+---------+----------+-------------+

Response Properties:
+--------+---------+-------------+
| Name   | Type    | Description |
+--------+---------+-------------+
| Result | Integer |             |
+--------+---------+-------------+
```

The signature line — `nn_PacxSum(Addend1: Integer, Addend2: Integer) -> Result: Integer` — is LLM-friendly: paste it into a Copilot prompt and it can generate calling code without any additional context.

### Generate a sample input file

```powershell
pacx customapi describe -n nn_PacxSum --generate-input-file C:\Temp\pacxsum-input.json
```

Produces:

```json
{
	"Addend1": 0,
	"Addend2": 0
}
```

This file can be passed directly to `customapi run --input-file`. For complex types like `Entity` or `EntityCollection`, PACX generates the appropriate JSON structure. For a JSON Schema, use `--generate-schema-file` instead.

---

## Step 4 — Write the Plugin

The plugin reads parameters using their `uniquename` as the key in `context.InputParameters`. This is a critical detail: Dataverse does **not** use the human-readable `name` field — it uses the `uniquename` of the `customapirequestparameter` entity.

```csharp
public class PacxSumPlugin : IPlugin
{
    private const string InAddend1 = "Addend1";   // not "nn_PacxSum-in-Addend1"
    private const string InAddend2 = "Addend2";
    private const string OutResult = "Result";    // not "nn_PacxSum-out-Result"

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)
            serviceProvider.GetService(typeof(IPluginExecutionContext));

        int addend1 = GetInt(context, InAddend1);
        int addend2 = GetInt(context, InAddend2);

        context.OutputParameters[OutResult] = addend1 + addend2;
    }

    private static int GetInt(IPluginExecutionContext context, string key)
    {
        if (!context.InputParameters.Contains(key))
            throw new InvalidPluginExecutionException($"Required input parameter '{key}' is missing.");
        return (int)context.InputParameters[key];
    }
}
```

> **Gotcha #1:** The `customapirequestparameter.uniquename` is the SDK message parameter key — even if it contains hyphens, which the docs warn against for regular uniquenames. Use the exact value from the `describe` output.

---

## Step 5 — Push the Plugin

The assembly must be strong-name signed for Dataverse:

```powershell
# Generate key once
sn.exe -k PacxIntegration.snk

# Add to .csproj
<SignAssembly>true</SignAssembly>
<AssemblyOriginatorKeyFile>PacxIntegration.snk</AssemblyOriginatorKeyFile>

# Build and push
dotnet build -c Release
pacx plugin push -p .\bin\Release\net462\PacxIntegration.dll
```

Output:

```
Creating assembly PacxIntegration (1.0.0.0)...Done
Adding assembly PacxIntegration (1.0.0.0) to solution master...Done
Creating plugin type PacxIntegration.PacxSumPlugin...Done
```

> **Gotcha #2:** Unsigned assemblies fail with "Public assembly must have public key token." Always sign before pushing.

---

## Step 6 — Bind the Plugin

```powershell
pacx customapi bind -a nn_PacxSum -p PacxIntegration.PacxSumPlugin
```

Output:

```
Resolving Custom API 'nn_PacxSum'...Done
Resolving plugin type 'PacxIntegration.PacxSumPlugin'...Done
Binding 'nn_PacxSum' to plugin 'PacxIntegration.PacxSumPlugin'...Done
Custom API 'nn_PacxSum' is now bound to plugin 'PacxIntegration.PacxSumPlugin'.
```

This sets the `plugintypeid` lookup on the `customapi` record — no Plugin Registration Tool required.

---

## Step 7 — Run It

### Inline JSON

```powershell
pacx customapi run -n nn_PacxSum --input '{"Addend1":5,"Addend2":3}'
```

Output:

```
Connecting to the current dataverse environment...Done
Resolving Custom API 'nn_PacxSum'...Done
Executing 'nn_PacxSum'...Done

+--------+-------+
| Name   | Value |
+--------+-------+
| Result | 8     |
+--------+-------+
```

**5 + 3 = 8.** It works.

### From a file

```powershell
# Edit the generated sample file
'{"Addend1":12,"Addend2":30}' | Set-Content C:\Temp\pacxsum-input.json

pacx customapi run -n nn_PacxSum --input-file C:\Temp\pacxsum-input.json
```

Output:

```
Result | 42
```

**12 + 30 = 42.** File-based input confirmed.

---

## Bugs Encountered and Fixed

This integration test was run on the actual development branch, so we hit real issues. Documenting them here for anyone building similar tooling.

### Bug 1: `description` cannot be NULL

The `customapi`, `customapirequestparameter`, and `customapiresponseproperty` entities all require a non-null `description`. The fix: default to `string.Empty` when the user doesn't supply one.

### Bug 2: `name` is a required field on `customapi`

The `name` attribute on `customapi` is the primary name column — required by Dataverse even though it's separate from `uniquename`. Fix: set `name = displayName` on the entity before saving.

### Bug 3: Wrong `ComponentType` enum values

Adding Custom API components to a solution via `AddSolutionComponentRequest` requires the correct `componenttype` integer. After iterative testing:

| Component                   | `componenttype` |
| --------------------------- | --------------- |
| `customapi`                 | 10036           |
| `customapirequestparameter` | 10037           |
| `customapiresponseproperty` | 10038           |

Earlier guesses (431/432/433, then just wrong) produced errors like "Cannot add AttributeImageConfig / CatalogAssignment". The correct values were determined by reading the Dataverse error message — each error names the component type that the integer maps to.

### Bug 4: SDK key is `uniquename`, not `name`

`OrganizationRequest.Parameters` uses the `customapirequestparameter.uniquename` as the key — not the `name` field. When PACX was using `name` ("Addend1"), Dataverse returned:

```
Unrecognized request parameter: Addend1
```

The fix: PACX now accepts user input JSON keyed by the short `name` (user-friendly), then maps to `uniquename` when building the `OrganizationRequest`. The plugin code uses the full `uniquename` directly.

---

## Full Command Reference

```powershell
# Create
pacx customapi create -d "My API" -p "Param1:String" -r "Result:Integer" [-s MySolution]

# List
pacx customapi list [-s MySolution]

# Describe
pacx customapi describe -n nn_MyApi
pacx customapi describe -n nn_MyApi --generate-input-file sample.json
pacx customapi describe -n nn_MyApi --generate-schema-file schema.json

# Add parameters/responses to existing API
pacx customapi add-param -a nn_MyApi -n NewParam -t String
pacx customapi add-response -a nn_MyApi -n NewProp -t Integer

# Bind plugin
pacx customapi bind -a nn_MyApi -p MyNamespace.MyPlugin

# Run
pacx customapi run -n nn_MyApi --input '{"Param1":"value"}'
pacx customapi run -n nn_MyApi --input-file payload.json

# Delete (with cascade to params/responses)
pacx customapi delete -n nn_MyApi [--force]
```

---

## Summary

The `customapi` command suite in PACX compresses what used to be a 15-minute multi-form exercise into a handful of terminal commands. The full lifecycle — create, describe, push plugin, bind, run — takes under two minutes once you know the pattern.

Key things to remember:

1. Plugin code uses `uniquename` (e.g. `"nn_PacxSum-in-Addend1"`) as the `InputParameters` key
2. `customapi run` input JSON uses the short `name` (e.g. `"Addend1"`) — PACX handles the mapping
3. Plugin assemblies must be strong-name signed
4. Always run `describe` before writing plugin code — the signature line gives you exact key names

The source code for the PACX CLI is at [github.com/neronotte/Greg.Xrm.Command](https://github.com/neronotte/Greg.Xrm.Command).
