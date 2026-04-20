# Catalog Commands

Commands for managing Dataverse Catalog and Business Events.

## catalog publish-item

Publish an item to the Dataverse Catalog for Business Events.

```bash
# Publish a catalog item
pacx catalog publish-item --name "new_MyCatalogItem"

# Publish as Business Event
pacx catalog publish-item --name "new_MyBusinessEvent" --type BusinessEvent

# Publish with description and version
pacx catalog publish-item --name "new_MyItem" --description "My catalog item" --version "1.0.0"

# Dry run (preview without creating)
pacx catalog publish-item --name "new_MyItem" --dry-run
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--name` | `-n` | **Required.** Name of the catalog item | - |
| `--type` | | Type: Catalog, BusinessEvent | Catalog |
| `--description` | | Description of the catalog item | None |
| `--version` | | Version of the catalog item | "1.0.0" |
| `--dry-run` | | Preview without creating | false |

### Notes

- Catalog items are used to expose Dataverse events externally
- Business Events are a specialized type of catalog item for triggering automated flows
- When published, the item starts in Draft state