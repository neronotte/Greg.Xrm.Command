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

### ✅ 10. Update translation documentation and run regression tests

- Document descriptions, Boolean labels, statecode, and statuscode.
- Explicitly exclude record-content translations handled by `pacx data create`.
- Run the full build and core test suite.

### ✅ 11. Implement system view translations

- Keep `view translate` aligned with the existing CLI namespace.
- Add `views translate` as a plural compatibility alias.
- Resolve a system view by name and optional table name.
- Translate the localized `name` and `description` attributes through
  `SetLocLabelsRequest`.
- Validate all LCIDs before updating the view.
- Publish the owning table unless `--no-publish` is supplied.
- Add command parsing, request-shape, publishing, and failure tests.

### ✅ 12. Translate global option set metadata

- Extend `optionset translate` without adding a new CLI namespace.
- Make `--value` and `--labels` optional when translating global metadata.
- Add `--display-name` and `--description` for global option sets.
- Keep value translation available in the same invocation when `--value` and
  `--labels` are provided.
- Reject global metadata options for local choices, statecode, and statuscode.
- Update metadata through `UpdateOptionSetRequest` with `MergeLabels = true`.
- Preserve support for global option values and local/state/status values.
- Add command validation, request-shape, publishing, and regression tests.

### ✅ 13. Add solution translation package export/import

- Place the commands in `Commands/Solution/Translations` so the folder matches
  the `Greg.Xrm.Command.Commands.Solution.Translations` namespace.
- Add a dedicated `NamespaceHelperBase` helper for the new
  `solution translation` verb namespace.
- Add `pacx solution translation export` and `pacx solution translation import`.
- Export translations via `ExportTranslationRequest` and write the returned zip
  to disk.
- Support `--unpack` on export so the returned archive is expanded into a
  sibling folder.
- Support importing either a zip file or a folder path.
- Support `--repack` on import so a folder can be bundled into a zip before the
  `ImportTranslationRequest` is issued.
- Poll the generated `importjob`, report its progress, and keep the command
  running until the job is complete.
- Reject a folder path when `--repack` is not supplied, before connecting to
  Dataverse.
- Validate that the target folder or archive exists and that the payload is not
  empty.
- Add parser and executor tests covering success and failure paths.
- Update the user-facing docs with the new CLI syntax and expected behavior.

### ✅ 14. Add non-blocking translation import

- Add the `--nowait` (`-nw`) flag to `solution translation import`.
- Keep polling enabled by default.
- When `--nowait` is supplied, return immediately after Dataverse accepts the
  `ImportTranslationRequest`.
- Return the import job ID so callers can monitor it independently.
- Add parser and executor tests proving that no polling request is made.
- Document the non-blocking behavior.

### ✅ 15. Resolve the default solution during translation export

- Make the export `--solution` option optional.
- Use the current default solution when the option is omitted.
- Fail before exporting when neither an explicit nor a default solution exists.
- Use the resolved solution name for the request, default file name, output, and
  command result.
- Add parser and executor tests for explicit, default, and missing solutions.
- Document the default-solution behavior.

## Definition of done

- All three commands are discoverable through the command registry.
- Invalid LCIDs are rejected before metadata updates begin.
- Existing labels for unspecified languages are preserved.
- Publishing defaults to enabled and can be disabled with `--no-publish`.
- Unit tests cover parser, language validation, command validation, requests,
  publishing, and failure paths.
