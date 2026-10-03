# Security role assignment and revocation

## Implementation Plan

### ✅ 1. Commands and validation

- Add `security roles assign` (aliases `add`, `associate`) and `security roles revoke` (aliases `remove`, `disassociate`).
- Require `--role` and at least one of `--user` or `--team`; allow both.
- Accept user GUID/domain/email, team GUID/name, and business unit GUID/name.
- Add local parser and validation tests, including aliases and short options.

### ✅ 2. Business unit resolution and idempotent execution

- Read the environment's record ownership across business units setting.
- If enabled, require `--businessunit` and resolve the role in that business unit.
- Otherwise ignore `--businessunit` and resolve the role in each recipient's business unit.
- Resolve all recipients and roles before writing; reject missing or ambiguous matches.
- Check direct user/team role associations and only associate/disassociate when necessary.
- Cover business unit selection, no-op cases, both recipients, lookup failures, faults and cancellation with mocked Dataverse tests.

### ✅ 3. Verification and documentation

- Run focused tests, build and the complete local test suite.
- Document examples and direct-assignment semantics.

Verification: all 122 new parser/executor tests pass and the solution builds in Release.
The full suite has 1184 passing tests and one failure in the existing `CheckPrivilegeCommandExecutorTest.ExecuteAsyncOnRecordShouldReturnAccessRightsInResult`, caused by a pre-existing worktree change returning a string instead of a privilege collection. That change was left untouched.
No live Dataverse environment was modified or used for verification.

## Usage

```powershell
pacx security roles assign --role "Salesperson" --user john.doe@contoso.com
pacx security roles assign --role "Salesperson" --team "Sales" --businessunit "Europe"
pacx security roles revoke --role "Salesperson" --user john.doe@contoso.com --team "Sales" --businessunit "Europe"
```

User assignments are direct: roles inherited through team membership are not changed.
When record ownership across business units is disabled, `--businessunit` is ignored.
Already assigned roles (assign) and absent assignments (revoke) are reported as no-ops.

## Options and Behavior

| Option           | Short | Accepted values                                                                  |
| ---------------- | ----- | -------------------------------------------------------------------------------- |
| `--role`         | `-r`  | Required exact role name, or GUID of the role copy in the selected business unit |
| `--user`         | `-u`  | System user GUID, domain name, or primary email                                  |
| `--team`         | `-t`  | Team GUID or exact team name                                                     |
| `--businessunit` | `-bu` | Business unit GUID or exact name                                                 |

Specify at least one recipient. Both `--user` and `--team` can be used in the same invocation.
The singular noun `security role` is also supported for the commands and their aliases.

The commands read `EnableOwnershipAcrossBusinessUnits` from the organization's `orgdborgsettings` XML.
If enabled, `--businessunit` is mandatory even when using a role GUID; the selected business unit applies to both recipients.
If disabled (including an absent setting), each recipient's business unit is used; `--businessunit` is ignored without looking it up.
Use a role name when targeting recipients in different business units: a role GUID selects an exact copy and must belong to the business unit being resolved.

Missing or ambiguous users, teams, roles, and business units are rejected before any writes.
Owner teams and Microsoft Entra group teams are supported. Access teams cannot have roles and are rejected.
Managed roles can be assigned or revoked: their definitions are not modified.

All recipients, role copies, and direct associations are resolved before writing.
Changes are applied sequentially, not transactionally. If a later write fails, the failure reports how many changes completed; rerunning safely skips successful changes.
Dataverse still enforces the executing identity's privileges and role-assignment restrictions.

The structured result contains `ChangedCount`, `SkippedCount`, and, for each specified recipient, `UserId`/`TeamId`, `UserRoleId`/`TeamRoleId`, `UserBusinessUnitId`/`TeamBusinessUnitId`, and `UserChanged`/`TeamChanged`.
