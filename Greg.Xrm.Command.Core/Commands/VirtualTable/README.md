# Virtual Table Commands

Commands for managing Virtual Tables in Dataverse.

## virtual-table scaffold

Scaffold virtual table definitions from external data sources.

```bash
# Scaffold from SQL Server
pacx virtual-table scaffold --datasource "new_MySqlConnection" --table "dbo.MyTable"

# Scaffold with custom table name
pacx virtual-table scaffold --datasource "new_MySqlConnection" --table "dbo.MyTable" --name "new_MyVirtualTable"

# Scaffold with prefix
pacx virtual-table scaffold --datasource "new_MySqlConnection" --table "dbo.MyTable" --prefix "vw"

# Output to file
pacx virtual-table scaffold --datasource "new_MySqlConnection" --table "dbo.MyTable" --output "./virtualtable.yml"
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--datasource` | `-d` | **Required.** External data source name | - |
| `--table` | `-t` | **Required.** Source table name | - |
| `--name` | `-n` | Virtual table logical name (auto-generated) | From source |
| `--prefix` | `-p` | Column name prefix | None |
| `--output` | `-o` | Output file path | stdout |
| `--format` | `-f` | Output format: yaml, json | yaml |

### Notes

- Virtual tables connect to external data sources
- Supported sources: SQL Server, Azure SQL, Cosmos DB, etc.
- Generates table definition YAML for review before creating
- Run `pacx virtual-table create` after reviewing the scaffold