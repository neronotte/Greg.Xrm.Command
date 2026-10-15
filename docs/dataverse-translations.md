# Dataverse translations

The translation commands accept numeric LCIDs and validate them against
languages that are installed and active in the current Dataverse organization.

## Column

```powershell
pacx column translate `
  --table account `
  --column new_customername `
  --labels '1033:"Customer name", 1040:"Nome cliente"'
```

## Table

```powershell
pacx table translate `
  --table account `
  --singular '1033:"Account", 1040:"Azienda"' `
  --plural '1033:"Accounts", 1040:"Aziende"'
```

The singular and plural arguments must contain the same LCIDs.

## Option set

Global option set:

```powershell
pacx optionset translate `
  --name new_status `
  --value 100000000 `
  --labels '1033:"Pending", 1040:"In attesa"'
```

Local choice:

```powershell
pacx optionset translate `
  --table account `
  --column new_status `
  --value 100000000 `
  --labels '1033:"Pending", 1040:"In attesa"'
```

## Publishing

Metadata is published by default. Use `--no-publish` to update metadata without
publishing it:

```powershell
pacx column translate ... --no-publish
```

The commands merge labels, so translations for languages not included in the
command are preserved.

Use the following command to inspect the languages available in the current
organization:

```powershell
pacx org language list
```
