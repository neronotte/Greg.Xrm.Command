# Security Role Get Privileges

Status: implemented and locally verified. No live Dataverse operations performed.

## Goal

Display a security role's privilege configuration in two sections:

- Table privileges: one row per table, eight privilege columns.
- Miscellaneous privileges: a three-column table with label, level, and technical name.

The command is read-only and accepts both managed and unmanaged roles. It shows
the selected role's assigned privileges, not effective user access, sharing,
field security, or privileges inherited from other user/team roles.

## CLI Contract

Primary command: `pacx security roles get-privileges`.

All six confirmed command forms are equivalent:

```text
pacx security roles get-privileges
pacx security role get-privileges
pacx security roles get
pacx security role get
pacx security roles getPrivileges
pacx security role getPrivileges
```

Register the first form as the primary command and the remaining five as
aliases. Do not register the primary form again as an alias. Every form accepts
the same options and invokes the same executor.

| Option | Short | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `--role` | `-r` | Yes | None | Exact root role name or GUID; same resolution as set-privilege. |
| `--table` | `-t` | No | None | Case-insensitive LIKE contains filter on a table's logical name, schema name, or display name; suppresses miscellaneous output. |
| `--privilege` | `-p` | No | None | Case-insensitive literal substring of a technical privilege name or standard action label. |
| `--show` | `-s` | No | `Assigned` | `All`, `Assigned`, or `Unassigned`. |
| `--format` | `-f` | No | `functional` | Four table formats and three JSON formats; see the format table below. |

`--format` replaces the previous compact boolean option. Do not expose
`--compact`, `--COMPACT`, or `-c`. Format names are case-insensitive;
`c` normalizes to `compact`, `n` to `number`, `t`/`tech` to `technical`,
and `f`/`func` to `functional`. These are option values, not boolean flags:
for example, `--format c` and `-f c` select compact output.
JSON aliases are `jsontech`/`jsontechnical`/`jt`,
`json`/`jsonfunc`/`jsonfunctional`/`jf`, and `jsonnumeric`/`jn`.
The role selector is again `--role`/`-r`; `--name`/`-n` is no longer accepted.
The assignment selector is `--show`/`-s`; `--mode`, `--filter`, and `-m` are no
longer accepted. `--show` chooses what to display; `--format`/`-f` chooses how.
Omitting the option selects `functional`. Empty or unknown format values are
validation errors, not a fallback to the default.

Examples:

```powershell
pacx security roles get-privileges -r "Salesperson"
pacx security roles get-privileges -r "Salesperson" --show All
pacx security roles get-privileges -r "Salesperson" -s Unassigned
pacx security roles get-privileges -r "Salesperson" --table account
pacx security roles get-privileges -r "Salesperson" --table "claim" --show All
pacx security roles get-privileges -r "Salesperson" --privilege Read
pacx security roles get-privileges -r "Salesperson" --privilege prvWriteAccount
pacx security roles get-privileges -r "Salesperson" --show All --format compact
pacx security roles get-privileges -r "Salesperson" --format number
pacx security roles get-privileges -r "Salesperson" --format tech
pacx security roles get-privileges -r "Salesperson" --format functional
pacx security roles get-privileges -r "Salesperson" --format c
pacx security roles get-privileges -r "Salesperson" -f n
pacx security roles get-privileges -r "Salesperson" --show All -f jt
pacx security roles get-privileges -r "Salesperson" --show All -f json
pacx security roles get-privileges -r "Salesperson" --show All -f jn
```

Names of modes are case-insensitive; undefined enum values are rejected by
command validation. The table filter is a case-insensitive LIKE contains search:
`--table "claim"` is equivalent to `LIKE '%claim%'` and matches both
`new_claims` and `new_claimresponse`. The privilege filter remains a literal
substring search, not a regular expression.

## Data And Classification

Reuse `SecurityRoleService.GetRolesByIdentifierAsync` to resolve the role.
Missing roles and ambiguous root names produce an error; GUIDs can identify a
specific business-unit copy. Unlike modification commands, managed roles are
not rejected.

Read the selected role's privileges with `RetrieveRolePrivilegesRoleRequest`.
Index the response by `PrivilegeId`. A missing privilege means not assigned,
not an invalid table/privilege combination.

Read table metadata once with `RetrieveAllEntitiesRequest`, requesting
`EntityFilters.Entity | EntityFilters.Privileges`. Use published metadata for
this read-only security view. Build the table map using
`SecurityPrivilegeMetadata.PrivilegeId` and `PrivilegeType`; never classify
privileges by parsing `prv...` names.

Extend the existing `Privilege.Repository` with a paged `GetAllAsync` query for
the `privilege` catalog, including technical names and supported-depth flags.
Miscellaneous privileges are catalog entries not mapped to one of the eight
table actions by metadata. Metadata entries with `PrivilegeType.None` belong
to the miscellaneous list, following RoleEditor's classification.

The eight actions, in fixed order:

1. Create
2. Read
3. Write
4. Delete
5. Append
6. Append To
7. Assign
8. Share

The table universe consists of tables having at least one of these supported
actions. Tables with no security actions are not presented as fully unassigned
tables. Do not reuse RoleEditor's hard-coded table exclusions or activity
grouping: this command shows individual tables. Shared privilege IDs may
legitimately appear in multiple table rows; classification is many-to-many.

Unexpected metadata action types or assigned IDs absent from the catalog must
not disappear silently. Preserve their technical name (or GUID if unavailable)
as an explicitly unclassified miscellaneous entry with a warning. Do not
substitute zero for an unknown depth.

## View Model

Build a presentation-independent snapshot before formatting:

- Role identity: GUID, name, business unit, managed/unmanaged.
- Table row: logical name, display name, and eight cells.
- Cell: applicability, privilege GUID/name, assigned depth, and filter match.
- Miscellaneous item: privilege GUID/name, assigned depth, and supported levels.

Keep non-applicability separate from unassigned status. Convert SDK depths
explicitly to the existing RoleEditor numeric mapping:

| Number | Meaning |
| --- | --- |
| 0 | Available privilege, not assigned to this role |
| 1 | Basic / User |
| 2 | Local / BusinessUnit |
| 3 | Deep / ParentChild |
| 4 | Global / Organization |

Do not cast `PrivilegeDepth` directly to these numbers. If the same assigned
privilege appears more than once in the response, retain the widest supported
depth and do not produce duplicate rows/items.

## Filtering Semantics

Mode selection is table-level, not cell-level:

| Mode | Table rows | Miscellaneous items |
| --- | --- | --- |
| Assigned | At least one of the eight actions is assigned | Assigned privileges only |
| All | Every table in the security-action universe | Every privilege |
| Unassigned | None of the eight actions is assigned | Unassigned privileges only |

`Unassigned` does not mean tables with any missing action. A table having Read
but no Write still belongs to Assigned, not Unassigned. This definition is
confirmed: the role must have NONE of the table's eight actions assigned.

Evaluate this table predicate using the complete eight-action configuration,
before applying the privilege-name filter. This keeps `--show` consistent:
filtering for Write does not turn a table with assigned Read into an unassigned
table. The displayed Write cell may therefore be unassigned in Assigned mode
(`0` in numeric formats or `None` in textual formats).

Then apply the filters:

- `--table` restricts table rows using LIKE contains matching and suppresses
  the entire miscellaneous section. This applies in every mode, including
  compact output, and even if no table matches.
- Without `--table`, `--privilege` restricts miscellaneous items by technical
  name. With `--table`, no miscellaneous items are included at all.
- For table rows, `--privilege` matches each cell's technical name or action
  label. Keep only rows with at least one applicable matching cell.
- Combine table and privilege filters with AND for table rows.
- Filter names against the full privilege catalog, not assigned privileges
  alone, so filtering also works in Unassigned mode.

In the full grid, show only action columns with at least one matching cell in
the resulting table rows. Keep their normal relative ordering. Without a
privilege filter, always show all eight columns.

Empty results are successful and produce a short section-specific message.
When `--table` is supplied, never print a miscellaneous heading or an empty
miscellaneous-results message.
Sort tables by logical name and miscellaneous items by technical privilege
name, case-insensitively. Display names are supplementary, not identifiers.

## Output

For table formats, print active table/name filters and warnings before the role
identity. Print a blank line immediately after the `Role: ...` heading, followed
by the grid. Do not print `Mode: ... | Format: ...` or `Table privileges`.
Render through `IOutput`; omit miscellaneous when `--table` is supplied. Never
write directly to Console. JSON formats use the separate export path below.

### Formats

| `--format` value | Layout | Unassigned | Basic depth | Local depth | Deep depth | Global depth |
| --- | --- | --- | --- | --- | --- | --- |
| `c` or `compact` | Eight-character string under `CRWDATaS` | 0 | 1 | 2 | 3 | 4 |
| `n` or `number` | Standard grid with eight action columns | 0 | 1 | 2 | 3 | 4 |
| `t`, `tech`, or `technical` | Standard grid with eight action columns | None | Basic | Local | Deep | Global |
| `f`, `func`, or `functional` (default) | Standard grid with eight action columns | None | User | Business Unit | Parent Child | Organization |
| `jsontech`, `jsontechnical`, `jt` | JSON object with string values | None | Basic | Local | Deep | Global |
| `json`, `jsonfunc`, `jsonfunctional`, `jf` | JSON object with string values | None | User | Business Unit | Parent Child | Organization |
| `jsonnumeric`, `jn` | JSON object with numeric values | 0 | 1 | 2 | 3 | 4 |

All standard grids retain the same table rows and action ordering. The existing
privilege filter may reduce visible columns. In every format, an unsupported
table/action combination remains blank; it must never be displayed as None or
zero. A supported but unassigned privilege is rendered using the Unassigned
column above. A cell excluded by the privilege filter uses `-` if its column
remains visible because that column matches another table row.

Functional grid, the default (illustrative data only):

```text
Table   | Create | Read         | Write         | Delete | Append       | Append To    | Assign | Share
account | None   | User         | Business Unit | None   | Organization | Organization | None   | None
contact | User   | Organization | None          | None   | User         | User         | None   | None
example |        | Organization |               |        |              |              |        |
```

Technical grid for the same data:

```text
Table    | Create | Read   | Write | Delete | Append | Append To | Assign | Share
account  | None   | Basic  | Local | None   | Global | Global    | None   | None
contact  | Basic  | Global | None  | None   | Basic  | Basic     | None   | None
example  |        | Global |       |        |        |           |        |
```

Number grid for the same data:

```text
Table   | Create | Read | Write | Delete | Append | Append To | Assign | Share
account | 0      | 1    | 2     | 0      | 4      | 4         | 0      | 0
contact | 1      | 4    | 0     | 0      | 1      | 1         | 0      | 0
example |        | 4    |       |        |        |           |        |
```

Compact grid (same illustrative data):

```text
Table    | CRWDATaS
account  | 01204400
contact  | 14001100
example  |  4
```

Every compact cell string has exactly eight characters, including trailing
spaces. Never trim it. Uppercase A is Append, T is Append To, lowercase a is
Assign, and S is Share. The header uses `CRWDATaS` to avoid duplicate symbols.

With a privilege filter the compact string keeps all eight positions:

```text
Table    | CRWDATaS
account  | -1------
```

Here `--privilege Read` selected Read only. `-` means excluded by the filter,
not unavailable or unassigned. An unsupported action remains a space even if
it would otherwise be excluded by the filter. Print a concise legend once.

Without `--table`, miscellaneous output is a three-column table in every table format.
Use the same level-label mapping as the grid; compact uses numeric labels.
The label removes the leading `prv` prefix, then inserts a space before every
uppercase character after the first. For example, `prvReadRecordAuditHistory`
becomes `Read Record Audit History`. The third column preserves the original
technical name unchanged. If there are no miscellaneous matches, show the
existing empty-results message instead of an empty table.
For example, the default functional format produces:

```text
Miscellaneous privileges
Label                      | Level        | Technical name
Export To Excel            | Organization | prvExportToExcel
Read Record Audit History  | Organization | prvReadRecordAuditHistory
Example User Privilege     | User         | prvExampleUserPrivilege
Example Unassigned Privilege | None       | prvExampleUnassignedPrivilege
```

Color may reinforce assigned levels when supported by the existing output
abstraction, but names/digits must remain sufficient in plain-text output.
Avoid adding a new rendering framework. Format selection does not change
classification, filtering, assignment state, or the selected miscellaneous items.

Tabular command result parameters contain only scalar role identity, `Show`, canonical
format, `TableCount`, `MiscellaneousCount`, and `WarningCount`. Never return arrays
or collections in output parameters: the CLI displays their .NET type names.
The full snapshot stays internal and feeds the tables. Warning messages are
printed through IOutput; only their count is included in the result.
When `--table` is supplied, `MiscellaneousCount` is zero and the section remains
suppressed. `security roles list` similarly returns a formatted role-name string,
not a collection, in its `Roles` output parameter.

### JSON Export

JSON formats emit one indented JSON object, not an array. Properties are the
technical privilege names and values use the selected level mapping. Shared
privilege IDs appear only once. Properties are sorted by technical name.

```json
{
  "prvExportToExcel": "Organization",
  "prvReadAccount": "User",
  "prvWriteAccount": "None"
}
```

The numeric counterpart uses actual JSON numbers, including unassigned zero:

```json
{
  "prvExportToExcel": 4,
  "prvReadAccount": 1,
  "prvWriteAccount": 0
}
```

The assignment filter keeps its table-level semantics. Assigned tables may
therefore contribute unassigned actions with value None/0. `--show All` also
includes wholly unassigned tables and miscellaneous privileges. `Unassigned`
exports only wholly unassigned tables and unassigned miscellaneous privileges.
Table and privilege-name filters apply before export; unsupported actions and
cells excluded by the privilege filter are omitted rather than emitted as empty
properties. `--table` suppresses miscellaneous entries in JSON too.

Empty results produce `{}`. Unknown depths produce JSON `null`, never zero or
an invented depth. JSON serialization preserves and escapes technical names.
The executor emits no progress logging, role heading, grid, legend, warning
text or result summary in JSON mode. It returns an empty successful
CommandResult so the generic runner does not append a `Result:` block. Global
CLI bootstrap banners are outside this executor and are not changed here.

## Implementation Plan

### ✅ 1. Approve The Contract

- Confirmed: Unassigned means tables with no assigned privilege at all.
- Confirmed: `--table` hides miscellaneous and uses LIKE contains matching;
  `claim` must include `new_claims` and `new_claimresponse`.
- Confirmed: the six command forms listed above, with `security roles
  get-privileges` primary and the remaining forms as aliases.
- Confirmed: `--format` replaces the compact boolean, with `c`/`compact`,
  `n`/`number`, `t`/`tech`/`technical`, and `f`/`func`/`functional`;
  default is `functional`.
- Confirmed: JSON technical (`jsontech`, `jsontechnical`, `jt`), functional
  (`json`, `jsonfunc`, `jsonfunctional`, `jf`), and numeric (`jsonnumeric`, `jn`).
- Confirmed: role selector `--role`/`-r`; assignment selector `--show`/`-s`.
- Approved for implementation, including Assigned as the mode default and `-`
  for compact cells excluded by the privilege filter.

### ✅ 2. Retrieve And Classify

- Add command/options and usage examples using the existing command pattern.
- Extend the existing privilege repository with a paged catalog read.
- Add one role-privilege inspection service next to the existing security
  services; reuse role resolution without changing unrelated methods.
- Test role lookup, managed read access, SDK requests, pagination, shared IDs,
  duplicate assignments, non-applicable cells, and miscellaneous classification
  with local mocks. No external services.

### ✅ 3. Filter And Render

- Keep classification/filtering separate from formatting; all seven output formats
  consume the same snapshot.
- Add number, technical, and functional grids, compact strings, miscellaneous
  table, identity, and legend. Use one level-label mapping for both tables.
- Test all three modes, every filter and their combinations, case-insensitivity,
  LIKE contains matching for table names, numeric conversion, deterministic
  ordering, empty results, and lossless eight-character strings.
- Verify that `--table claim` includes `new_claims` and `new_claimresponse`,
  excludes `new_case`, and behaves identically for `CLAIM`. Verify miscellaneous
  suppression in every mode, in compact output, combined with `--privilege`,
  and when the table filter has no matches.
- Test all seven formats, every accepted name/alias and casing, the functional
  default, unassigned labels, blank unsupported cells, and invalid/empty formats.
- Explicitly test short format values `c`, `n`, `t`, and `f` with both
  `--format` and `-f`, including uppercase values and canonical normalization.
- Test usage documentation and parser long/short names, `--format`/`-f`,
  rejection of the obsolete compact flag, defaults, aliases, and invalid modes.
  Reuse existing test helpers and OutputToMemory.
- Test scalar-only result parameters and the three miscellaneous columns in
  every format, including prefix removal, uppercase word boundaries, unknown
  depths, and unassigned levels. Capture table rows to verify internal data.
- Add parameterized parser tests for all six confirmed command forms, verifying
  that they resolve to the same command type and bind options identically.
- Parse JSON exports to verify object shape, string versus integer values,
  filtering, shared-ID deduplication, all levels, null unknown depths, empty
  results, and absence of command-specific logging or summary.

### ✅ 4. Verify

- Run focused tests, full local suite, editor diagnostics, and Release build.
- No live environment writes; no commits or remote pushes without authorization.

## Local Design Check

The proposed flow is consistent with the existing role resolver, IOutput grid
API, and RoleEditor's metadata-based classification. Implementation acceptance
must include a metadata fixture where a privilege's technical name suggests
one table but its PrivilegeId is mapped to another/shared table. A name-prefix
implementation must fail this test; a metadata-based implementation must pass.

## Verification Results

- 107 focused command and executor tests passed, including --role/-r and
  --show/-s binding, rejection of superseded --name/-n, --mode, --filter, and -m options,
  parsed JSON exports, and tabular heading spacing without redundant logging.
- Full local suite: 1063 tests passed, no failures.
- Release solution build passed; no editor diagnostics in the new files.
- Tests cover all six command forms, format aliases/defaults, modes, metadata-ID
  classification, shared IDs, paged privilege reads, managed-role inspection,
  LIKE contains table filtering, miscellaneous suppression, numeric/text depth
  mapping, fixed-width compact strings, unknown depths, and service faults.
- Existing work was committed as `ecc4077` and pushed to
  `origin/feature/security-namespace` before implementation, as requested.
- The get-privileges implementation follows that baseline in a separate
  delivery commit. No real-environment security operations were run.