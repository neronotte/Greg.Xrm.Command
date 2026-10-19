# Teams and User Security Profile

## Implementation Plan

### ✅ 1. Queries and Commands

- Add `security teams list` with optional `--type`/`-t` and name-contains `--name`/`-n` filters. Without a type filter, return all team types.
- Add `security teams get roles` with required `--team`/`-t` (GUID or exact name), plus singular-team and get-roles/getRoles aliases.
- Add `user profile`, also under `users` and `security user/users`, with optional `--user`/`-u` (current user by default) and `--format`/`-f` Tree or Json.
- Profile output includes the user's business unit, direct and inherited roles, membership teams, and each team's assigned roles, preserving IDs and business units.
- Reuse existing repositories, paging helpers, user resolution, role queries, and Spectre tree rendering. Batch team role reads rather than querying each team separately.
- Add local parser/executor tests covering filtering, paging, ambiguous identifiers, inheritance, empty results, output formats, cancellation, and faults.

### ✅ 2. Verification and Documentation

- Run focused new tests and neighboring security tests, then the complete local suite and Release build.
- Document syntax, team type values, JSON use, and Entra group membership synchronization limits.

Latest verification: all 20 profile executor tests pass, including direct/team separation, ANSI colors, and cancellation before connection; the complete local suite passes all 1,364 tests. User profile and team-role executors now check cancellation before output or connection. The six duplicate service copies under `Commands/Security` have been removed; compilation and tests pass without source exclusions. Tree rendering is exercised using a real Spectre console captured to memory; Dataverse queries are mocked. No live environment was read or modified.

## Usage

```powershell
pacx security teams list --type Owner --name Sales
pacx security teams get roles --team "Sales"
pacx user profile --user john.doe@contoso.com
pacx user profile -u john.doe@contoso.com -f Json --nologo
```

## Teams

`pacx security teams list` accepts optional `--type`/`-t` and `--name`/`-n` filters. Name matching is case-insensitive contains; filtering is performed by Dataverse. Every result page is read. Omit the type filter to include all team types.

| Type              | Numeric Value |
| ----------------- | ------------- |
| Owner             | 0             |
| Access            | 1             |
| SecurityGroup     | 2             |
| Microsoft365Group | 3             |

The listing shows team ID, name, type, and business unit. Unknown type values remain visible numerically when no filter is used.

`pacx security teams get roles --team <name-or-GUID>` lists roles assigned directly to the selected team, including role ID, name, business unit, and managed status. Missing or ambiguous team names are rejected; use a GUID to disambiguate. A team with no roles returns a successful empty result, including access teams.

Aliases: `security team list`; `security team get roles`; `security teams/team get-roles`; `security teams/team getRoles`.

## User Profile

`pacx user profile` accepts `--user`/`-u` as a user GUID, domain name, or primary email. If omitted, the current connected user is selected. Aliases: `users profile`, `security user profile`, `security users profile`.

`--format`/`-f` accepts Tree (default) or Json. Tree displays the user's business unit, directly assigned roles, and every membership team with its assigned roles. Roles inherited through a team appear only under that team, not in the user's Roles section. The tree starts after a blank line and follows the interactive palette: labels are SkyBlue2, names and values SandyBrown, and identifiers/counts Gray. Teams with no roles and empty role/membership lists are shown explicitly. Names are escaped before Spectre rendering.

JSON contains `UserId`, `FullName`, `DomainName`, `BusinessUnit` (ID/name, or null if absent), `Roles` (direct role metadata, with `Sources: ["Direct"]`), and `Teams` (team metadata and their roles). Role metadata includes both role ID and business unit ID. Direct roles are deduplicated by ID, not name, so copies in different business units remain distinct. A shared role can appear under multiple teams. It also appears in the user's Roles section only when independently assigned directly to the user.

JSON mode emits no command progress or result summary. Use `--nologo` to suppress the standard PACX banner when piping JSON into another tool. Failures do not emit an incomplete JSON profile.

Membership and role queries read all pages. Team roles are retrieved in one shared query rather than one query per team. Microsoft Entra group-team membership reflects Dataverse synchronization: users who have never accessed the environment may have incomplete membership information. No directory synchronization or writes are performed by these commands.
