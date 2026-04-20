# Custom API Commands

Commands for managing Custom APIs (Custom Actions) in Dataverse.

## custom-api create

Create a Custom API with input/output parameters.

```bash
# Create a simple global Custom API
pacx custom-api create --name "new_MyAction"

# Create with display name and description
pacx custom-api create --name "new_MyAction" --display-name "My Action" --description "Does something useful"

# Create with input/output parameters
pacx custom-api create --name "new_MyAction" --input "String:Target" --input "Int:Count" --output "Entity:Result"

# Create entity-bound Custom API
pacx custom-api create --name "new_MyAccountAction" --binding-type Entity --entity account

# Create as a function (read-only)
pacx custom-api create --name "new_GetData" --is-function

# Create with plugin execution
pacx custom-api create --name "new_MyAction" --execute-plugin "MyPlugin.MyPluginType"

# Add to specific solution
pacx custom-api create --name "new_MyAction" --solution "MySolution"
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--name` | `-n` | **Required.** Unique name of the Custom API | - |
| `--display-name` | | Display name | Name value |
| `--description` | | Description | Empty |
| `--input` | | Input parameter (Type:Name). Repeatable. | None |
| `--output` | | Output parameter (Type:Name). Repeatable. | None |
| `--binding-type` | | Binding: Global, Entity, EntityCollection | Global |
| `--entity` | `-e` | Entity logical name for Entity/EntityCollection binding | None |
| `--solution` | `-s` | Solution unique name | None |
| `--execute-plugin` | | Plugin type name to execute | None |
| `--is-function` | | Mark as function (read-only) | false |

### Supported Parameter Types

- `String` (10)
- `Int`/`Integer` (6)
- `Bool`/`Boolean` (7)
- `DateTime` (8)
- `Decimal`/`Money` (5)
- `Double` (4)
- `Guid`/`Uniqueidentifier` (11)
- `Entity`/`EntityReference` (1)
- `Picklist`/`Optionset` (2)
- `StringArray` (13)

## custom-api list

List all Custom APIs in the current environment.

```bash
# List all Custom APIs (table format)
pacx custom-api list

# List as JSON
pacx custom-api list --format json
pacx custom-api list -f json

# Filter by bound entity
pacx custom-api list --entity account
pacx custom-api list -e account
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--format` | `-f` | Output format: table, json | table |
| `--entity` | `-e` | Filter by bound entity logical name | None |

## custom-api delete

Delete a Custom API from Dataverse.

```bash
# Delete by name
pacx custom-api delete --name "new_MyAction"
pacx custom-api delete -n "new_MyAction"
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--name` | `-n` | **Required.** Unique name of the Custom API | - |

### Notes

- Deleting a Custom API also deletes all its input/output parameters
- This operation cannot be undone
