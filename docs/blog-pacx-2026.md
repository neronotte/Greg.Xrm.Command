# PACX in 2026: Growing Alongside the Agent-First Era

Microsoft's investments in making Power Platform development increasingly **Agent-First** are changing the way we work every day. The [video by Nick Doelman](https://www.youtube.com/watch?v=uGgeGO5TMA4) is just the tip of the iceberg: agents are not a feature anymore, they are the interface. And where there are agents, there are Dataverse tables, Custom APIs, plugin steps, web resources, solutions — all the things that pro-code developers have been building for years, now at the center of a much larger stage.

PACX has always had one goal: **simplify the developer's daily life by reducing the need to open the Maker Portal**. Every time you fire up a browser to create a column, register a plugin step, or check which solution components are misplaced, you are losing flow. PACX is the CLI that gives that flow back.

In 2026, PACX has grown significantly. This post walks through the most relevant additions, grouped by theme.

---

## A Better Terminal Experience

Before covering the new commands, it is worth mentioning the foundation they were built on. In March 2026, **Spectre.Console** was introduced as the rendering backbone for the entire CLI. This was not a cosmetic change: it brought a consistent, color-coded, interactive output layer to every command — progress spinners, structured tables, rich error messages. If you used PACX before and found the output a bit raw, this is the release that changed that.

---

## Organization & Language Management

```powershell
pacx org info
pacx org language list
pacx org language add --language 1040
pacx org language remove --language 1040
```

Four new commands under [`pacx org`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org) to manage your Dataverse organization without touching the admin portal. [`org info`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org-info) gives you a quick summary of the current environment. The [`org language`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org-language) group lets you [`list`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org-language-list), [`add`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org-language-add), and [`remove`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-org-language-remove) languages programmatically — useful in automated environment setup scripts, or when you are managing multilingual deployments and want that step reproducible and auditable.

---

## Column Naming Conventions

```powershell
pacx column conventions set --prefix myp --casing camelCase
pacx column conventions show
pacx column conventions reset
```

PACX already supported creating columns of any type from the CLI. What was missing was a way to enforce consistency. The new [`column conventions`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-column-conventions) command group lets you configure the naming rules once — prefix, casing style — and have every subsequent `column create` command respect them automatically. [`set`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-column-conventions-set) applies the convention, [`show`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-column-conventions-show) tells you what is active, [`reset`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-column-conventions-reset) brings you back to defaults. Small command, large impact on team consistency.

---

## Solution Component Management

```powershell
pacx solution component add --solution MySolution --type 1 --id <guid>
pacx solution component move --from SolutionA --to SolutionB --type 1 --id <guid>
pacx solution component moveAll --from SolutionA --to SolutionB
pacx solution component remove --solution MySolution --type 1 --id <guid>
pacx solution component removeAll --solution MySolution
pacx solution component types
pacx solution component addTable --solution MySolution --table contact
```

A full suite under [`pacx solution component`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component) for managing solution components. The [`types`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-types) command is particularly handy when you are scripting: it lists all the numeric component type codes so you do not have to look them up. [`addTable`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-addTable) is a convenience wrapper that adds a full table to a solution in one step. [`moveAll`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-moveAll) is the command you reach for when reorganizing a messy solution landscape — move everything from one solution to another, scripted and repeatable. The full set also includes [`add`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-add), [`remove`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-remove), [`removeAll`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-removeAll), and [`move`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-component-move).

---

## Generating Constants from Metadata

```powershell
pacx solution constants --solutionName MySolution --outputCs ./constants --namespaceCs MySolution.Constants
pacx solution constants --solutionName MySolution --outputJs ./constants --namespaceJs MySolution
```

If you are writing plugin code or PCF controls and you are still hardcoding entity logical names and attribute names as string literals, [`solution generateLateBoundConstants`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-solution-generateLateBoundConstants) is the answer. It reads your solution's metadata and emits a constants file — either C# or JavaScript — with all table names, attribute names, entity set names, and global option set values. Regenerate it after every schema change and your code stays in sync without manual effort.

---

## User Settings Management

```powershell
pacx usersettings list
pacx usersettings set --uilanguageid 1040

175 user setting definitions, manageable from the terminal via [`pacx userSettings`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-usersettings). If you have ever needed to toggle a specific Dataverse user setting across environments as part of a deployment or test setup, this is the command group for that. [`list`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-usersettings-list) shows everything available; [`set`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-usersettings-set) changes a value for the current user on the current environment.

---

## Custom API — Full Lifecycle Management

This is the biggest addition of the first half of 2026. Custom APIs are at the heart of the Agent-First model: they are how you expose your business logic to Copilot Studio agents, to connectors, to low-code automation. Being able to manage them entirely from the CLI is no longer a nice-to-have.

```powershell
# Create a global Custom API
pacx customapi create --unique-name myp_MyAction --display-name "My Action" --solution MySolution

# Create a bound Custom API (entity binding)
pacx customapi create --unique-name myp_MyAction --display-name "My Action" \
  --binding-type Entity --bound-entity contact --solution MySolution

# Add parameters and response properties
pacx customapi add-param --api myp_MyAction --param InputName:String
pacx customapi add-response --api myp_MyAction --response OutputResult:String

# Inspect it
pacx customapi list --full
pacx customapi describe --unique-name myp_MyAction --generate-input-file --generate-schema-file

# Execute it
pacx customapi run --api myp_MyAction --input input.json

# Bind to a plugin
pacx customapi bind --api myp_MyAction --plugin MyPlugin.MyType

# Clean up
pacx customapi removeParam --api myp_MyAction --name InputName
pacx customapi delete --api myp_MyAction
```

The [`pacx customapi`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi) group covers the full lifecycle. A few details worth highlighting:

- [`create`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-create): `--unique-name` is optional — if omitted, PACX infers it from the display name and the solution publisher prefix. The `--execute-privilege` option uses **fuzzy matching**: you do not need to know the exact privilege name, PACX will find the closest match.
- [`list`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-list): the `--full` flag displays the complete API signature — parameter names, types, directions — so you can audit what is deployed at a glance.
- [`describe`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-describe): generates an input JSON template and a JSON schema file locally, ready to use with [`run`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-run) or with any HTTP client.
- [`bind`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-bind) wires the API to a plugin type. [`addParam`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-add-param) / [`removeParam`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-remove-param) and [`addResponse`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-add-response) / [`removeResponse`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-customapi-remove-response) let you evolve the API contract incrementally without recreating it from scratch.

---

## Plugin Step Image Attributes

```powershell
pacx plugin step register ... \
  --preImageAttributes firstname,lastname,emailaddress1 \
  --postImageAttributes firstname,lastname
```

A focused addition to the existing [`plugin step register`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-plugin-step-register) command. Previously, you could register pre- and post-images but you had to retrieve all attributes. Now you can declare exactly which attributes to include in each image. When specified, `--preImage` and `--postImage` are activated automatically — no need to set them separately. Smaller payloads, faster plugin execution, less noise in your step traces.

---

## Web Resource Theme — Modern CustomThemeDefinition

```powershell
# Set the org-level logo for the first time
pacx wr setEnvImage --file mylogo.png --color "#0078D4"

# Update the logo for a specific app
pacx wr setEnvImage --file mylogo.png --appName "My Model App"

# Sync the theme to a local file for version control
pacx wr setEnvImage --file mylogo.png --localThemeFile ./theme.xml
```

The [`webresources setEnvImage`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-webresources-setEnvImage) command (alias `setLogo`, `setOrgImage`) was fully rearchitected, migrating from the legacy image settings API to the modern `CustomThemeDefinition` flow. The new `--localThemeFile` option is particularly relevant for teams that version-control their environment configuration: PACX will keep the local file in sync with what is deployed, so your theme is reproducible across environments.

---

## Data — A Full CRUD Suite

The most impactful set of additions lands at the end of the timeline. Between July 16 and July 19, 2026, PACX gained the ability to **query, create, update, and delete Dataverse records** directly from the terminal.

The story behind it is worth telling. The [Dataverse MCP server](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-mcp) is a natural companion for anyone working in an Agent-First setup: it exposes Dataverse operations as tools that an AI agent can invoke. In theory, it covers the full CRUD surface. In practice, I kept hitting errors like this one:

```
Failed to validate tool mcp_microsoft_dat_delete_record:
Error: tool parameters array type must have items.
Please open an issue for the MCP server or extension which provides this tool.
```

At some point the question becomes obvious: **why go through the Dataverse MCP server for CRUD when PACX can do it directly, reliably, and with full control over the output?** The MCP server is great for agent-to-Dataverse orchestration, but for a developer working at the terminal — scripting a data migration, seeding a test environment, fixing a bad record — the CLI is the better tool. No JSON schema validation quirks, no tool registration issues, no middleware between you and the API.

So the CRUD suite was born. The goal is **feature parity with the Dataverse MCP server's data operations**, and we are not far from it.

### Query

```powershell
# FetchXML — inline
pacx data query -q "<fetch><entity name='account'><attribute name='name'/></entity></fetch>"

# SQL
pacx data query -q "SELECT name, accountid FROM account WHERE statecode = 0"

# OData
pacx data query -q "accounts?`$select=name,accountid&`$filter=statecode eq 0&`$top=10"

# From file — same flag, regardless of what's inside
pacx data query --query-file ./queries/my-query.xml -f Excel -o ./results.xlsx --auto-run
```

Here is the part that makes [`pacx data query`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-data-query) genuinely elegant: **there is a single parameter, `-q`, and you just pass your query in.** PACX figures out the rest.

Under the hood, `QueryExecutorFactory.DetectExecutorFromQueryText()` applies three heuristics in sequence:

| Condition | Query type detected |
|---|---|
| Text starts with `<` | FetchXML |
| Text starts with `SELECT ` (case-insensitive) | SQL (TDS endpoint) |
| Text contains `$filter=`, `$select=`, `$top=`, … (strict regex) | OData |

No flags, no `--type fetchxml`, no mode switching. You write the query in whatever language you know — or whichever one makes sense for the task at hand — and PACX routes it to the right engine. The three executors all return the same `IReadOnlyCollection<Entity>`, so the rest of the pipeline — output formatting, pagination, console rendering — is completely agnostic to how the data was fetched.

The OData path deserves a special mention: the raw HTTP response is normalized before it hits the formatter. Lookup fields (returned as `_fieldname_value` properties) are reconstructed as proper `EntityReference` instances; OData formatted values (choice labels, date strings) are mapped into the `FormattedValues` dictionary, exactly as they would appear in a native SDK call. The result is a consistent entity model regardless of which query language you used.

Output formats — JSON (default), CSV, XML, Excel — are available for all three query types. Add `--output ./file.xlsx --auto-run` and the file opens automatically when the query completes.

### Create & Update

```powershell
# Plain input
pacx data create -t contact --plain "firstname=Mario;lastname=Rossi;gendercode=1"

# JSON inline
pacx data create -t contact --json '{"firstname":"Mario","lastname":"Rossi"}'

# From file, returning specific columns
pacx data create -t contact --file ./payload.json --return firstname,lastname

# With explicit GUID
pacx data create -t contact --file ./payload.json --id 3fa85f64-5717-4562-b3fc-2c963f66afa6

# Update
pacx data update -t contact --id <guid> --plain "jobtitle=Developer" --return jobtitle

# Dry-run: validate payload without writing
pacx data create -t contact --file ./payload.json --dry-run
```

Both [`data create`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-data-create) and [`data update`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-data-update) support 10+ field types: strings, integers, decimals, money, booleans, dates, single and multi-select choices, and lookups. Lookups can be resolved by GUID (`account(3fa85f64-...)`) or by field value (`account(name='Contoso')`), with PACX querying Dataverse at runtime to resolve the reference. The `--dry-run` flag validates the entire payload and reports any type conversion errors before touching the environment.

### Delete

```powershell
# Delete a record
pacx data delete -t contact --id 3fa85f64-5717-4562-b3fc-2c963f66afa6

# Preview before deleting
pacx data delete -t contact --id 3fa85f64-5717-4562-b3fc-2c963f66afa6 --dry-run
```

[`data delete`](https://github.com/neronotte/Greg.Xrm.Command/wiki/pacx-data-delete) always retrieves the record first, confirming it exists before attempting deletion. In `--dry-run` mode it shows the record's primary name attribute so you can verify you are targeting the right record. Clean, safe, auditable.

---

## The Bigger Picture

Looking at this list as a whole, a pattern emerges. The commands added in 2026 are not isolated utilities — they cluster around the same scenarios that the Agent-First model makes critical:

- **Custom APIs** are how agents call your business logic.
- **Data CRUD** is how agents (and the automation around them) read and write state.
- **Solution components and constants** are how you keep the schema under control as it evolves faster than ever.
- **Plugin step images** are how you make sure your server-side logic stays efficient as the volume of agent-triggered operations scales up.

PACX will keep growing along the same line: every time you find yourself reaching for a browser to do something that could be a command, that is a gap worth closing.

You can find the full command reference in the [PACX wiki](https://github.com/neronotte/Greg.Xrm.Command/wiki). Feedback, issues, and pull requests are welcome.
