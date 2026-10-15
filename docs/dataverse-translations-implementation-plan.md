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

### ✅ 8. Extend table and column metadata translations

- Add table descriptions to `table translate`.
- Add column descriptions to `column translate`.
- Add True/False labels for Boolean columns to `column translate`.
- Validate metadata types before updating Boolean labels.
- Add command and executor tests.

### ✅ 9. Extend option set translations to state and status

- Keep global option sets supported by `optionset translate`.
- Support local `statuscode` through `UpdateOptionValueRequest`.
- Support local `statecode` through `UpdateStateValueRequest`.
- Validate that state and status values exist before updating.
- Add request-shape and publishing tests.

### 🕒 10. Update translation documentation and run regression tests

- Document descriptions, Boolean labels, statecode, and statuscode.
- Explicitly exclude record-content translations handled by `pacx data create`.
- Run the full build and core test suite.

## Definition of done

- All three commands are discoverable through the command registry.
- Invalid LCIDs are rejected before metadata updates begin.
- Existing labels for unspecified languages are preserved.
- Publishing defaults to enabled and can be disabled with `--no-publish`.
- Unit tests cover parser, language validation, command validation, requests,
  publishing, and failure paths.
