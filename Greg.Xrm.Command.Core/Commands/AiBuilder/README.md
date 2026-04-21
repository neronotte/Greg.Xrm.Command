# AI Builder Commands

Commands for managing Microsoft AI Builder models in Dataverse.

## Prerequisites

### Required Permissions

- AI Builder license assigned to the user
- Environment maker or system customizer security role
- For form processor: access to the form/table data sources

## ai model list

List all AI Builder models with training status and accuracy.

```bash
# List all AI Builder models
pacx ai model list

# Filter by status
pacx ai model list --status Completed

# Output as JSON
pacx ai model list --format json
pacx ai model list -f json
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--format` | `-f` | Output format: table, json | table |
| `--status` | `-s` | Filter by training status: NotStarted, Training, Completed, Failed | - |

## ai model train

Trigger AI Builder model training from labeled data.

```bash
# Trigger training
pacx ai model train --model-id "model-guid-123"

# Trigger and wait for completion
pacx ai model train --model-id "model-guid-123" --wait
pacx ai model train -m "model-guid-123" -w
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--model-id` | `-m` | **Required.** AI Builder model ID to train | - |
| `--wait` | `-w` | Wait for training to complete (polling every 30s) | false |

### Notes

- The model must have labeled training data before triggering training
- Use `--wait` to poll for completion, or check status manually with `ai model list`
- Training time varies based on model complexity and data volume

## ai model publish

Publish a trained AI Builder model to an environment.

```bash
# Publish a model
pacx ai model publish --model-id "model-guid-123"
pacx ai model publish -m "model-guid-123"

# Dry run (show what would be published)
pacx ai model publish --model-id "model-guid-123" --dry-run
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--model-id` | `-m` | **Required.** AI Builder model ID to publish | - |
| `--dry-run` | | Show what would be published without actually publishing | false |

### Notes

- Model must be trained successfully before publishing
- Publishing makes the model available for use in Power Apps and Dataverse

## ai form-processor configure

Configure form processing model (document type, fields, tables).

```bash
# Configure form processor with fields
pacx ai form-processor configure --model-id "model-guid-123" --doc-type "Invoice" --fields "InvoiceNumber,Date,Amount,Vendor"

# Configure with fields and tables
pacx ai form-processor configure -m "model-guid-123" -d "Invoice" -f "InvoiceNumber,Date,Amount" -t "LineItems"
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--model-id` | `-m` | **Required.** Form processing model ID | - |
| `--doc-type` | `-d` | **Required.** Document type name | - |
| `--fields` | `-f` | Comma-separated list of field names to extract | - |
| `--tables` | `-t` | Comma-separated list of table names to extract | - |

## Troubleshooting

### "AI Builder model not found"
Verify the model ID is correct and exists in your environment.

### "User does not have permission to access AI Builder"
Ensure you have an AI Builder license and appropriate Dataverse security roles.

### "Training failed"
Check the model has sufficient labeled training data. Review training error details in the AI Builder UI.

### "Publishing failed"
Ensure the model training completed successfully before publishing.
