# Connection Reference Commands

Commands for managing Connection References across solutions.

## connection-ref map

Map connection references across solutions and environments.

```bash
# List connection references in a solution
pacx connection-ref map --solution "new_MySolution"

# Map from source to target solution
pacx connection-ref map --source "SourceSolution" --target "TargetSolution"

# Interactive mode
pacx connection-ref map --solution "new_MySolution" --interactive
pacx connection-ref map -s "new_MySolution" -i

# Dry run mode
pacx connection-ref map --source "Dev" --target "Test" --dry-run
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--solution` | `-s` | Solution unique name | Current |
| `--source` | | Source solution name | None |
| `--target` | | Target solution name | None |
| `--interactive` | `-i` | Interactive mapping mode | false |
| `--dry-run` | | Preview changes without applying | false |
| `--format` | `-f` | Output format: table, json | table |

### Notes

- Connection references link to connection records
- Used to switch connections between environments
- Interactive mode allows step-by-step mapping
- Dry run shows proposed changes without applying them