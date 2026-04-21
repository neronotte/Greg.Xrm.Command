# Plugin Commands (spkl Parity)

Attribute-based plugin registration and flexible web resource mapping - providing spkl-like functionality.

## Commands

### Plugin Registration (Attribute-Based)

- `plugin register-attributes` — Scan DLLs for plugin attributes and register in Dataverse
- `plugin step-scan` — Validate plugin steps without deployment

### Plugin Step Management

- `plugin step list` — List registered plugin steps
- `plugin step register` — Register a single plugin step
- `plugin step unregister` — Unregister a plugin step
- `plugin step enable` — Enable a plugin step
- `plugin step disable` — Disable a plugin step

### Plugin Type Management

- `plugin type list` — List registered plugin types
- `plugin type unregister` — Unregister a plugin type

### Other

- `plugin list` — List all registered plugins
- `plugin push` — Push plugin changes to Dataverse

### Web Resources

- `webresource map` — Define file-to-resource mapping via YAML/JSON config
- `webresource watch` — File watcher that syncs changes to Dataverse on save

---

## Plugin Register Attributes

Scan compiled DLLs for `[CrmPluginStep]`, `[CrmPluginImage]`, and `[CrmWebhook]` attributes and register in Dataverse.

### Usage

```bash
# Register plugins from a DLL file
pacx plugin register-attributes --path ./bin/MyPlugin.dll

# Register plugins from a directory
pacx plugin register-attributes --path ./bin/

# Dry run (validate without deploying)
pacx plugin register-attributes --path ./bin/ --dry-run

# Output as JSON
pacx plugin register-attributes --path ./bin/ --format json

# Specify publisher
pacx plugin register-attributes --path ./bin/ --publisher MyPublisher --publisher-name "My Publisher"
```

### Attributes

```csharp
using Greg.Xrm.Command.Interfaces;

[CrmPluginStep(" account", "Create", Stage = SdkMessageStage_type.Prevalidation, 
    ExecutionMode = SdkMessageStep_mode.Synchronous, FilteringAttributes = "name")]
public class AccountPlugin : IPlugin
{
    public void Execute(IServiceProvider provider) { ... }
}

[CrmPluginImage("PreImage", ImageTypeEnum.PreImage, "name,accountnumber")]
[CrmPluginImage("PostImage", ImageTypeEnum.PostImage, "name")]
public class AccountPluginWithImages : IPlugin { ... }
```

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|--------|
| `--path` | `-p` | Path to DLL or directory | Required |
| `--publisher` | | Publisher unique name | DefaultPublisher |
| `--publisher-name` | Publisher display name | |
| `--dry-run` | `-d` | Validate without deploying | false |
| `--format` | `-f` | Output format: table, json | table |

---

## Plugin Step Scan

Validate plugin steps without deploying to Dataverse.

### Usage

```bash
# Scan current directory
pacx plugin step-scan

# Scan specific path
pacx plugin step-scan --path ./bin/

# Filter by assembly
pacx plugin step-scan --path ./bin/ --assembly MyPlugin

# Output as JSON
pacx plugin step-scan --path ./bin/ --format json
```

### Validation Rules

- Stage is Prevalidation, PreOperation, PostOperation, or PostExternal
- Execution mode is Synchronous or Asynchronous
- Filtering attributes are valid for Create/Update/Delete
- Message name is valid

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|--------|
| `--path` | `-p` | Path to DLL or directory | Current |
| `--assembly` | `-a` | Filter by assembly name | All |
| `--format` | `-f` | Output format: table, json | table |

---

## Web Resource Map

Define file-to-resource mapping via JSON config.

### Usage

```bash
# Map web resources from config
pacx webresource map --config webresources.json

# Dry run
pacx webresource map --config webresources.json --dry-run

# Specify solution
pacx webresource map --config webresources.json --solution MySolution
```

### Config Format

```json
{
  "mappings": [
    { "path": "web/script.js", "name": "new_script.js" },
    { "path": "web/styles.css", "name": "new_styles.css" }
  ]
}
```

---

## Web Resource Watch

File watcher that syncs changes to Dataverse on save.

### Usage

```bash
# Watch and sync
pacx webresource watch --config webresources.json

# Watch specific solution
pacx webresource watch --config webresources.json --solution MySolution

# Disable auto-publish
pacx webresource watch --config webresources.json --no-publish
```

---

## Examples

### Full Workflow

```bash
# 1. Build your plugin DLL
dotnet build ./MyPlugin

# 2. Scan and validate (dry run)
pacx plugin register-attributes --path ./bin/ --dry-run

# 3. Register in Dataverse
pacx plugin register-attributes --path ./bin/

# 4. Scan for validation errors
pacx plugin step-scan --path ./bin/

# 5. Set up web resource mapping
pacx webresource map --config webresources.json

# 6. Start file watcher
pacx webresource watch --config webresources.json
```

### Incremental Registration

The `register-attributes` command checks existing records by name and updates vs creates:

```bash
# First run - creates all
pacx plugin register-attributes --path ./bin/

# Second run - updates only changed
pacx plugin register-attributes --path ./bin/
```

## Exit Codes

- `0` — Success
- `1` — Error (path not found, validation failed, etc.)