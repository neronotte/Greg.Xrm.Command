# Microsoft Forms Commands

Commands for managing and exporting Microsoft Forms data.

## Prerequisites

### Azure AD App Registration

1. Register an Azure AD App in the Azure Portal
2. Grant API permissions:
   - `Forms.Read.All` (Application) - for user-owned forms
   - `Forms.ReadWrite.All` (Application) - for exporting
3. Create a client secret
4. Note the Client ID and Tenant ID

### Environment Variables

Set these before using Forms commands:

```bash
# Required for all commands
export MSAL_CLIENT_ID="your-azure-app-client-id"

# For user-owned forms (Client Credentials flow)
export MSAL_CLIENT_SECRET="your-client-secret"

# For group-owned forms (ROPC flow - requires MFA-excluded account)
export MSAL_USERNAME="service-account@contoso.onmicrosoft.com"
export MSAL_PASSWORD="your-password"
```

## forms list

List all Microsoft Forms with metadata.

```bash
# List forms for current user
pacx forms list --tenant contoso.onmicrosoft.com
pacx forms list -t contoso.onmicrosoft.com

# List specific user's forms
pacx forms list --tenant contoso.onmicrosoft.com --owner "user-id-123"
pacx forms list -t contoso.onmicrosoft.com -o "user-id-123"

# Output as JSON
pacx forms list --tenant contoso.onmicrosoft.com --format json
pacx forms list -t contoso.onmicrosoft.com -f json
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--tenant` | `-t` | **Required.** Tenant ID or domain (e.g., contoso.onmicrosoft.com) | - |
| `--owner` | `-o` | Owner user ID. If not provided, lists current user's forms | current user |
| `--format` | `-f` | Output format: table, json | table |
| `--token` | | OAuth2 access token (reads from MSAL cache if not provided) | auto |

## forms response count

Get quick count of responses for monitoring/alerting.

```bash
# Get response count for a form
pacx forms response count --tenant contoso.onmicrosoft.com --form-id "form-id-123"
pacx forms response count -t contoso.onmicrosoft.com -f "form-id-123"

# For a specific owner
pacx forms response count -t contoso.onmicrosoft.com -f "form-id-123" --owner "user-id-123"
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--tenant` | `-t` | **Required.** Tenant ID or domain | - |
| `--form-id` | `-f` | **Required.** Form ID to count responses for | - |
| `--owner` | `-o` | Owner user ID | current user |
| `--token` | | OAuth2 access token | auto |

## forms responses export

Export responses to CSV, JSON, or SQL with paged retrieval.

```bash
# Export to CSV (default)
pacx forms responses export --tenant contoso.onmicrosoft.com --form-id "form-id-123" --output responses.csv
pacx forms responses export -t contoso.onmicrosoft.com -f "form-id-123" -o responses.csv

# Export to JSON
pacx forms responses export -t contoso.onmicrosoft.com -f "form-id-123" -o responses.json --format json

# Export to SQL
pacx forms responses export -t contoso.onmicrosoft.com -f "form-id-123" -o responses.sql --format sql

# Incremental export (for repeated exports)
pacx forms responses export -t contoso.onmicrosoft.com -f "form-id-123" -o responses.csv --incremental
pacx forms responses export -t contoso.onmicrosoft.com -f "form-id-123" -o responses.csv -i
```

### Parameters

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--tenant` | `-t` | **Required.** Tenant ID or domain | - |
| `--form-id` | `-f` | **Required.** Form ID to export from | - |
| `--output` | `-o` | **Required.** Output file path (CSV, JSON, or SQL) | - |
| `--owner` | | Owner user ID | current user |
| `--format` | | Export format: csv, json, sql | csv |
| `--incremental` | `-i` | Only export responses since last export | false |
| `--token` | | OAuth2 access token | auto |

### Export Formats

**CSV**: Flat table with ResponseId, SubmittedAt, and answer columns
```csv
ResponseId,SubmittedAt,Question1,Question2
abc123,2024-01-15T10:30:00Z,Yes,Some answer
```

**JSON**: Structured JSON with all answers
```json
[
  {
    "ResponseId": "abc123",
    "SubmittedAt": "2024-01-15T10:30:00Z",
    "Answers": {
      "Question1": "Yes",
      "Question2": "Some answer"
    }
  }
]
```

**SQL**: INSERT statements for database import
```sql
CREATE TABLE IF NOT EXISTS form_responses (...);
INSERT INTO form_responses (...) VALUES (...);
```

## Authentication Flow

### User-Owned Forms (Client Credentials)

Use for forms you own personally:
```bash
export MSAL_CLIENT_ID="app-client-id"
export MSAL_CLIENT_SECRET="app-secret"
pacx forms list -t contoso.onmicrosoft.com
```

### Group-Owned Forms (ROPC)

Use for forms owned by groups or shared:
```bash
export MSAL_CLIENT_ID="app-client-id"
export MSAL_USERNAME="service-account@contoso.onmicrosoft.com"
export MSAL_PASSWORD="account-password"
pacx forms list -t contoso.onmicrosoft.com
```

> **Note**: ROPC requires a service account WITHOUT MFA enabled. This is a Microsoft limitation.

## Troubleshooting

### "MSAL_CLIENT_ID environment variable is required"
Set the environment variable before running the command.

### "AADSTS700016: Application not found"
Verify the Client ID is correct and the app is registered in your tenant.

### "AADSTS50034: User not found"
For group forms, ensure you're using ROPC with the correct username/password.

### Rate Limiting
The Forms API has rate limits. If you hit them, wait a minute and retry. The commands handle pagination automatically for large response sets.
