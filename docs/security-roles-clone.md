# Clone Security Roles

## Implementation Plan

### ✅ 1. Command and Clone Behavior

- Add `pacx roles clone`, with `roles copy`, `role clone`, and `role copy` aliases and equivalent `security` forms.
- Select the source by exact root role name or GUID; reject missing or ambiguous roles.
- Allow overriding name, description, business unit (`-bu`), and member privilege inheritance.
- Preserve unspecified description and inheritance from the source.
- Generate a unique environment-wide name: `Source - Copy`, `Source - Copy 2`, and so on.
- Allow a different business unit regardless of the record ownership across business units setting.
- Copy every privilege and depth; do not copy user or team assignments.
- Use existing model repositories and SDK messages; test queries, clone requests, overrides, naming, validation, faults, and cancellation locally.

### ✅ 2. Verification and Documentation

- Run focused command/executor tests and neighboring repository tests.
- Build in Release and run the full local suite.
- Document options, examples, and operational limitations.

Verification: all 49 clone parser/executor tests pass, and the solution builds in Release.
The full suite has 1252 passing tests and the existing failure in `CheckPrivilegeCommandExecutorTest.ExecuteAsyncOnRecordShouldReturnAccessRightsInResult` (string result cast to a privilege collection). No live Dataverse environment was used; actual cross-business-unit creation and transaction execution remain unverified against a server.

## Usage

```powershell
pacx roles clone --role "Salesperson"
pacx roles copy -r "Salesperson" -n "Regional Sales" -d "Regional sales permissions"
pacx role clone -r "Salesperson" -bu "Europe" -i TeamOnly
pacx security roles clone -r "Salesperson" -i DirectUserAndTeam
```

`roles copy`, `role clone`, and `role copy` are aliases. All four forms also work with the `security` prefix.

| Option           | Short | Behavior                                                                  |
| ---------------- | ----- | ------------------------------------------------------------------------- |
| `--role`         | `-r`  | Required exact root role name or GUID of a specific role copy             |
| `--name`         | `-n`  | Optional new name, at most 100 characters                                 |
| `--description`  | `-d`  | Optional description, at most 2000 characters; an empty value clears it   |
| `--businessunit` | `-bu` | Destination business unit GUID/name; defaults to the source business unit |
| `--inheritance`  | `-i`  | `TeamOnly`/`0` or `DirectUserAndTeam`/`1`; defaults to the source value   |

## Defaults and Safety

- Automatic names are `Source - Copy`, then `Source - Copy 2`, `Source - Copy 3`, and so on. Numbering continues above the highest matching suffix in the environment, even if earlier copies were deleted. Other business units and all query pages participate in the collision check. Long source names are shortened so the complete generated name stays within 100 characters.
- An explicitly provided name must not already exist in the environment; matching is case-insensitive.
- Omitted description and inheritance are preserved. If the source has no inheritance value, the Dataverse default `DirectUserAndTeam` is used.
- `TeamOnly` corresponds to **Team privileges only**. `DirectUserAndTeam` corresponds to **Direct User (Basic) access level and Team privileges** in the role editor.
- `--businessunit` is always honored, regardless of `EnableOwnershipAcrossBusinessUnits`. If omitted, the source role's business unit is retained. The command does not read the organization's settings.
- Managed source roles can be copied. The new role is an independent, unmanaged role; the source is never modified. System flags, solution membership, and user/team assignments are not copied.
- `RetrieveRolePrivilegesRoleRequest` reads the source privileges. A single `ExecuteTransactionRequest` creates the destination and applies `ReplacePrivilegesRoleRequest`, preserving privilege IDs and depths. If either write fails, the server rolls back the transaction; even an empty source privilege set replaces the new role's defaults.
- This recreates the role using supported SDK messages rather than the admin-center Copy Role dialog, which does not support copying directly to another business unit. Dataverse still enforces privileges and restrictions. Special behavior tied to built-in roles (for example System Customizer) is not guaranteed for their copies, as documented by Microsoft.
- Name selection is a preflight check, not a concurrency lock. Avoid simultaneous clones with the same desired name.

The result contains `SourceRoleId`, `RoleId`, `RoleName`, `BusinessUnitId`, `Inheritance`, and `PrivilegeCount`.
