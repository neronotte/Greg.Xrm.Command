# Security Role Privileges

## Implementation Plan

### ✅ 1. Define the command contract and verify SDK behavior

- Command: `pacx security roles set-privilege` (alias: `security role set-privilege`).
- Identify a role by exact name or GUID with `--role` / `-r`. Names resolve only root roles; ambiguous matches fail.
- Identify a privilege with either `--name` / `-n` or both `--table` / `-t` and `--privilege` / `-p`.
- Require `--level` / `-l`: 1/Basic, 2/Local, 3/Deep, 4/Global; 0, explicit empty or `null` removes the privilege. Omitting the level must not remove anything.
- Resolve the privilege against Dataverse's privilege table and validate its supported depth flags.
- Verify parser behavior and SDK update/removal semantics with local tests.

### ✅ 2. Implement privilege resolution and execution

- Add an EntityWrapper model and repository for privilege lookup.
- Reuse the existing role service for name resolution.
- Set only the selected privilege or remove it without altering unrelated role privileges.
- Add command help and usage examples.
- Test supported/unsupported levels, lookup failures, ambiguous roles, updates, removal, and service faults without external connections.

### ✅ 3. Validate and document

- Run focused command and executor tests, then the full local test suite.
- Build Release and check diagnostics.
- Document examples, levels, and removal behavior.
- No commits or remote pushes without explicit authorization.

## Usage

```powershell
pacx security roles set-privilege -r "Salesperson" -n prvWriteAccount -l Basic
pacx security roles set-privilege -r "Salesperson" -t Account -p Write -l Organization
pacx security roles set-privilege -r "Salesperson" -n prvWriteAccount -l 1
pacx security roles set-privilege -r "Salesperson" -n prvWriteAccount -l 0
pacx security roles set-privilege -r "Salesperson" -n prvWriteAccount -l null
pacx security roles set-privilege -r "Salesperson" -n prvWriteAccount -l ""
pacx security roles clear-privilege -r "Salesperson" -n prvWriteAccount
pacx security roles clear-privilege -r "Salesperson" -t Account -p Write
```

`security role set-privilege` is an alias. All options also accept long names:
`--role`, `--name`, `--table`, `--privilege`, `--level`.

`clear-privilege` accepts the same role and privilege selectors, without a level
option. It delegates to `set-privilege` with an explicit null level and preserves
the same privilege lookup, role resolution, and result fields.
`security role clear-privilege` is an alias of the plural command.

The role selector accepts an exact root role name or a GUID. Duplicate root names
are rejected; supply the GUID to disambiguate. A GUID can identify a role in a
specific business unit.

Only unmanaged roles can be modified. Both `set-privilege` and `clear-privilege`
reject managed roles immediately after role resolution, before privilege lookup
or any update/removal request. This applies whether the role is selected by name
or GUID and regardless of the requested level.

Use either the complete technical privilege name or a table name and action.
The latter builds `prv{action}{table}`, for example `Account` and `Write` become
`prvWriteAccount`. Use the table name as it appears in the technical privilege
name, not a localized display label. Both forms are validated against the
Dataverse `privilege` table, case-insensitively.

| Number | Level  | Alias               | Required privilege flag |
| ------ | ------ | ------------------- | ----------------------- |
| 0      | Remove | null or empty value | None                    |
| 1      | Basic  | User                | canbebasic              |
| 2      | Local  | BusinessUnit        | canbelocal              |
| 3      | Deep   | ParentChild         | canbedeep               |
| 4      | Global | Organization        | canbeglobal             |

Names of levels are case-insensitive. Numeric levels use the RoleEditor mapping
above, not the SDK enum's underlying integers. Unsupported levels (including 5)
are rejected before writing. Explicit 0, `null` or an empty value removes the
privilege, regardless of its supported depth flags. Omitting `--level` fails
validation and never removes a privilege. In shells that do not forward empty
native arguments, prefer `-l null` or `clear-privilege`.

DataAnnotations and `IValidatableObject` validation is performed by
`CommandRunnerBase` before executor invocation, not repeated in the executors.

The command sends `AddPrivilegesRoleRequest` for setting a depth and
`RemovePrivilegeRoleRequest` for removal, targeting only the selected privilege.
It does not replace the role's complete privilege collection. The active user
must have permission to modify the selected role.

This follows RoleEditor's
[Privilege model](https://github.com/neronotte/Greg.Xrm/blob/master/src/Greg.Xrm.RoleEditor/Model/Privilege.cs)
for allowed levels and
[ChangeSummary](https://github.com/neronotte/Greg.Xrm/blob/master/src/Greg.Xrm.RoleEditor/Views/Editor/ChangeSummary.cs)
for both adding privileges and changing the depth of an already assigned privilege.

Structured results include `RoleId`, `RoleName`, `PrivilegeId`, `PrivilegeName`,
`Level` (SDK depth name or `None`), and `Removed`.

## Verification

- 88 focused parser/executor tests passed for both commands, including numeric
  levels, namespace aliases, automatic executor registration, generated usage
  documentation, and managed-role rejection. Dataverse services are mocked;
  no external environment is contacted.
- Full suite: 956 tests passed, no failures. The two earlier list-command test
  failures have been corrected to match the existing executor result contracts.
- Release solution build passed.
- Earlier Release CLI smoke test recognized `security role set-privilege --help`;
  tests now verify that `roles` is primary and `role` is the alias for both commands.
- No editor diagnostics in the new command, executor, model, or tests.
- No live security role changes performed.

## Clear Privilege Implementation Plan

### ✅ 1. Add the removal-only command and tests

- Add `security roles clear-privilege` and the singular `security role` alias.
- Accept the same role and privilege selectors as `set-privilege`, without a level option.
- Delegate validation and execution to `set-privilege` with an explicitly null level.
- Test parsing, invalid selectors, forwarding, cancellation-token forwarding, and the SDK removal request with local mocks.
- Test automatic dependency injection, primary namespace, and numeric levels 0-4; reject 5.

### ✅ 2. Verify and document clear-privilege

- Run focused tests for both privilege commands and the full suite.
- Build Release, check editor diagnostics, and add usage examples.
- Do not change live roles, commit, or push.
