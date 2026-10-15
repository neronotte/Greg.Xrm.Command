# Dataverse translations implementation plan

## Scope

Add first-generation commands for setting localized Dataverse metadata labels:

- `pacx column translate`
- `pacx table translate`
- `pacx optionset translate`

The first version handles inline translations only. Bulk files and bulk-oriented
commands are intentionally out of scope.

## Translation syntax

Translations use a compact, non-JSON format:

```text
1033:"Customer", 1040:"Cliente"
```

Only numeric LCIDs are accepted. Labels may contain spaces, commas, colons, and
escaped quotes.

## Execution steps

### ✅ 1. Implement and test the translation parser

- Create a shared parser under `Services/Translations`.
- Parse `LCID:"label"` pairs, including quoted and escaped content.
- Reject malformed input, non-numeric LCIDs, duplicate LCIDs, empty labels, and
  trailing/empty entries.
- Add local unit tests for valid and invalid syntax.

### ✅ 2. Implement and test active Dataverse language validation

- Create a language resolver service under `Services/Translations`.
- Retrieve active `languagelocale` rows.
- Retrieve installed language packs.
- Retrieve provisioned languages.
- Include the organization base language when SDK responses omit it.
- Expose validation that accepts only installed and active LCIDs.
- Add unit tests for valid, inactive, uninstalled, unknown, and base-language
  LCIDs.

### ✅ 3. Implement `column translate`

- Add `TranslateCommand` and `TranslateCommandExecutor` under
  `Commands/Column`.
- Add table, column, labels, and `--no-publish` options.
- Retrieve attribute metadata and update `DisplayName`.
- Use `MergeLabels = true`.
- Publish the table unless `--no-publish` is supplied.
- Add command parsing, validation, and executor tests.

### ✅ 4. Implement `table translate`

- Add `TranslateCommand` and `TranslateCommandExecutor` under
  `Commands/Table`.
- Add table, singular labels, plural labels, and `--no-publish` options.
- Require matching LCID sets for singular and plural labels.
- Retrieve entity metadata and update `DisplayName` and
  `DisplayCollectionName`.
- Use `MergeLabels = true`.
- Publish the table unless `--no-publish` is supplied.
- Add command parsing, validation, and executor tests.

### ✅ 5. Implement `optionset translate`

- Add `TranslateCommand` and `TranslateCommandExecutor` under
  `Commands/OptionSet`.
- Support either a global option set or a local table/column option set.
- Require an existing option value.
- Update the option label with `MergeLabels = true`.
- Publish the global option set or table unless `--no-publish` is supplied.
- Reject `statecode` in the first version.
- Add command parsing, validation, and executor tests.

### ✅ 6. Register services and update documentation

- Register all supporting services in `IoCModule`.
- Document command usage and validation behavior.
- Ensure output and `CommandResult` contain only scalar values.

### ✅ 7. Validate the implementation

- Run targeted translation tests.
- Run `dotnet build`.
- Run the complete core test suite.
- Review the final diff for repository conventions and accidental changes.

## Definition of done

- All three commands are discoverable through the command registry.
- Invalid LCIDs are rejected before metadata updates begin.
- Existing labels for unspecified languages are preserved.
- Publishing defaults to enabled and can be disabled with `--no-publish`.
- Unit tests cover parser, language validation, command validation, requests,
  publishing, and failure paths.
