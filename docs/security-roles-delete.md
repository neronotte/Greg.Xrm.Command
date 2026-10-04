# Delete Security Roles

## Implementation Plan

### ✅ 1. Command and Managed Guard

- Add `pacx roles delete`, with `role delete` and equivalent `security` aliases.
- Require `--role`/`-r`: exact root role name or GUID.
- Load `ismanaged` in the existing role repository.
- Only delete roles whose `ismanaged` value is explicitly false; reject managed roles and unknown status without writes.
- Preserve cancellation and map Dataverse errors to command failures.
- Add local parsing, validation, executor, managed-guard, lookup, fault, cancellation, and DI tests.

### ✅ 2. Verification

- Run focused delete and neighboring clone tests.
- Build in Release and run the complete local suite.

Verification: all 20 new delete parser/executor tests pass, and the solution builds in Release.
The full suite has 1272 passing tests and the existing failure in `CheckPrivilegeCommandExecutorTest.ExecuteAsyncOnRecordShouldReturnAccessRightsInResult` (string result cast to a privilege collection). No live Dataverse environment was used or modified.

## Usage

```powershell
pacx roles delete --role "Salesperson - Copy"
pacx role delete -r 00000000-0000-0000-0000-000000000001
pacx security roles delete -r "Salesperson - Copy"
```

Only unmanaged roles can be deleted. The role must exist and resolve uniquely.
Names select root roles; use a GUID to select an exact role record when names are ambiguous.
The command deletes immediately, without a confirmation prompt, consistent with other PACX delete commands.
Deleting a role can remove access granted by that role to users and teams. Dataverse enforces privileges, dependencies, system-role restrictions, and cascading behavior.
No force flag or automatic dependency removal is provided.

The result contains `RoleId`, `RoleName`, and `Deleted`.
