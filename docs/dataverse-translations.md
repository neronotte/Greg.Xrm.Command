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

The global option set display name and description can be translated without
targeting a value:

```powershell
pacx optionset translate `
  --name new_status `
  --displayName '1033:"Contract status", 1040:"Stato contratto"' `
  --description '1033:"Available contract statuses", 1040:"Stati disponibili del contratto"'
```

Global metadata and an option value may be translated in the same invocation by
combining `--displayName` or `--description` with `--value` and `--labels`.

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

## Solution translation package

The solution-level translation flow is handled by the package commands rather than
by direct app/sitemap metadata operations:

```powershell
pacx solution translation export --solution MySolution --path C:\temp\MySolution.zip --unpack
pacx solution translation export --path C:\temp\CurrentSolution.zip
pacx solution translation import --path C:\temp\MySolution.zip
pacx solution translation import --path C:\temp\MySolutionFolder --repack
pacx solution translation import --path C:\temp\MySolution.zip --nowait
```

- `ExportTranslationRequest` is used to export the solution translation package.
- `--solution` is optional. When omitted, export uses the current default
  solution configured in PACX; the command fails if no default solution exists.
- `--unpack` expands the zip into a sibling folder next to the archive.
- `--repack` creates a zip from a folder before calling
  `ImportTranslationRequest`.
- `ImportTranslationRequest` expects the final compressed translation payload.
- After starting the import, the command polls the Dataverse `importjob` record,
  reports its progress, and returns only after the job has completed.
- `--nowait` skips polling and returns immediately after Dataverse accepts the
  import. The command result still includes `ImportJobId` for external
  monitoring.
- Passing a folder to `import` without `--repack` fails before any call to
  Dataverse is made.

## Scope exclusions

Email templates, knowledge articles, and other content records are not metadata
translation commands. They can be managed as records with `pacx data create`.

## System views

System view names and descriptions can be translated with the canonical
`view translate` command. `views translate` is available as a plural alias.

```powershell
pacx view translate `
  --name "Active Accounts" `
  --table account `
  --labels '1033:"Active Accounts", 1040:"Aziende attive"' `
  --description '1033:"Active customer accounts", 1040:"Aziende cliente attive"'
```

Personal views are not included because they are user-owned records rather than
solution-localized system view components.
