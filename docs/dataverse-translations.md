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

The same command can update the column description:

```powershell
pacx column translate `
  --table account `
  --column new_customername `
  --description '1033:"Customer account name", 1040:"Nome dell''azienda"'
```

For Boolean columns, the True and False labels can be translated as well:

```powershell
pacx column translate `
  --table account `
  --column new_ispreferred `
  --true-label '1033:"Yes", 1040:"Sì"' `
  --false-label '1033:"No", 1040:"No"'
```

## Table

```powershell
pacx table translate `
  --table account `
  --singular '1033:"Account", 1040:"Azienda"' `
  --plural '1033:"Accounts", 1040:"Aziende"'
```

The singular and plural arguments must contain the same LCIDs.

Table descriptions are supported through `--description`.

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

The same command handles local `statuscode` and `statecode` values:

```powershell
pacx optionset translate `
  --table account `
  --column statecode `
  --value 0 `
  --labels '1033:"Active", 1040:"Attivo"'
```

Global option sets continue to use `--name`; `statecode` and `statuscode` are
handled through the same `optionset translate` command rather than separate
commands.

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

## Scope exclusions

Email templates, knowledge articles, and other content records are not metadata
translation commands. They can be managed as records with `pacx data create`.
