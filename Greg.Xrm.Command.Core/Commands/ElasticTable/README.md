# Elastic Table Commands

Commands for managing Elastic Tables in Dataverse.

## elastic-table manage

Manage Elastic Table retention policies and scaling.

```bash
# Show current configuration
pacx elastic-table manage --table "new_MyElasticTable" --show
pacx elastic-table manage -t "new_MyElasticTable" -s

# Set retention period
pacx elastic-table manage --table "new_MyElasticTable" --retention "90d"
pacx elastic-table manage -t "new_MyElasticTable" -r "6m"

# Set scale capacity
pacx elastic-table manage --table "new_MyElasticTable" --scale "Large"
pacx elastic-table manage -t "new_MyElasticTable" --scale "Medium"

# Enable change feed tracking
pacx elastic-table manage --table "new_MyElasticTable" --changelog

# Show configuration as JSON
pacx elastic-table manage --table "new_MyElasticTable" --show --format json
pacx elastic-table manage -t "new_MyElasticTable" -s -f json
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--table` | `-t` | **Required.** Elastic table logical name | - |
| `--retention` | `-r` | Retention period (e.g., 90d, 6m, 1y) | None |
| `--scale` | `-s` | Scale capacity setting | None |
| `--changelog` | | Enable/disable change feed tracking | null |
| `--show` | | Show current configuration | false |
| `--format` | `-f` | Output format: table, json | table |

### Retention Period Format

- `90d` - 90 days
- `6m` - 6 months
- `1y` - 1 year
- `2y` - 2 years

### Scale Capacity Options

- `Small` - Standard capacity
- `Medium` - 1TB storage
- `Large` - 5TB storage

### Notes

- Elastic tables are stored in Azure Cosmos DB
- Retention policy controls data lifecycle
- Change feed enables near real-time streaming
- Requires Dataverse with Elastic table capability