# Solution Management Commands

Compare solutions and move components between solutions.

## Commands

- `solution diff` — Compare two solutions and show component differences
- `solution component-move` — Move a component from one solution to another

## Solution Diff

Compares components between two solutions.

### Usage

```bash
# Compare two solutions
pacx solution diff --source DefaultSolution --target NewSolution

# Compare specific component types
pacx solution diff --source DefaultSolution --target NewSolution --component-type Entity

# Output as JSON
pacx solution diff -s DefaultSolution -t NewSolution --format json
```

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|--------|
| `--source` | `-s` | Source solution unique name | Required |
| `--target` | `-t` | Target solution unique name | Required |
| `--component-type` | `-c` | Filter by component type | All |
| `--format` | `-f` | Output format: table, json | table |

### Supported Component Types

- Entity
- Attribute
- Relationship
- OptionSet
- PluginType
- WebResource
- Workflow
- SdkMessageProcessingStep
- CustomAPI

### Examples

```bash
# Compare solutions
pacx solution diff -s MySolution -t MySolution_managed

# Compare only entities
pacx solution diff -s MySolution -t MySolution_managed -c Entity

# JSON output for automation
pacx solution diff -s SolutionA -t SolutionB --format json
```

## Solution Component Move

Moves a component from one solution to another.

### Usage

```bash
# Move a component between solutions
pacx solution component-move --from DefaultSolution --to NewSolution --component-name Account --component-type Entity

# Dry run (preview)
pacx solution component-move -f DefaultSolution -t NewSolution -c Account -t Entity --dry-run

# Include dependencies
pacx solution component-move -f DefaultSolution -t NewSolution -c Account -t Entity --include-deps
```

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|--------|
| `--from` | `-f` | Source solution unique name | Required |
| `--to` | `-t` | Target solution unique name | Required |
| `--component-name` | `-c` | Component name to move | Required |
| `--component-type` | | Component type | Required |
| `--include-deps` | `-d` | Include required components | false |
| `--dry-run` | | Preview without making changes | false |

### Supported Component Types

- Entity
- Attribute
- Relationship
- OptionSet / Picklist
- Plugin / PluginType
- WebResource
- Workflow
- Step / SdkMessageProcessingStep
- Image / SdkMessageProcessingStepImage
- CanvasApp
- Connector
- ConnectionReference
- CustomAPI

### Examples

```bash
# Move an entity to another solution
pacx solution component-move -f Sandbox -t Base -c Account -t Entity

# Move with dependencies
pacx solution component-move -f Sandbox -t Base -c new_customentity -t Entity -d

# Preview what would happen
pacx solution component-move -f Sandbox -t Base -c Account -t Entity --dry-run
```

### Output

```
Connecting to the current Dataverse environment... Done
Adding Account to Base... Done
Removing Account from Sandbox... Done
Component 'Account' moved from 'Sandbox' to 'Base'.
```

## Exit Codes

- `0` — Success
- `1` — Error ( solution not found, component not found, etc.)