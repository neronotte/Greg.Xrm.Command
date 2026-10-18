# Issue 217: Authentication token and TDS implementation plan

## Goal

Fix long-running Dataverse commands that fail when an access token expires, stop persisting raw access tokens in `tokens.json`, and preserve a supported way to authenticate to the Dataverse TDS endpoint from `pacx data query --sql`.

## Mandatory invariants

- No regression to existing `pacx auth` profile workflows: create, select, list, ping, rename, delete, current/default profile, project profile, and environment override.
- OAuth interactive profiles continue to work across separate `pacx` processes without repeatedly prompting after initial authentication and refresh their tokens during long-running commands.
- SQL queries continue to authenticate to the TDS endpoint using a current token for the same profile/environment as the `ServiceClient` supplied to the query executor. This is an acceptance criterion, not a follow-up.
- Existing Certificate and ClientSecret authentication continues to work for Dataverse commands and TDS wherever currently supported; OAuth cache settings must not be applied to those profiles as if they were interactive OAuth.
- Explicit connection-string values, including an explicit username and explicit token-cache path, are preserved. Missing defaults are added once, without duplicate or malformed parameters.
- A failed connection, token refresh, or TDS login must produce an actionable failure. It must not silently switch to a token belonging to another profile or environment.
- PACX must not write newly obtained bearer tokens in plaintext. Existing `tokens.json` data must be retired through an explicit, tested migration or cleanup path.
- Keep public/plugin-facing contracts compatible unless a breaking API change is separately approved.

## Current behavior and risk

`OrganizationServiceRepository` appends `TokenCacheStorePath` using the cache directory, although the Dataverse SDK documents a full file path. It first tries the PACX `TokenCache`, which stores a raw access token in `tokens.json`, and creates a `ServiceClient` from a callback that returns that captured token. That callback does not provide a refresh-token flow for a client that outlives the access token.

`QueryExecutorSql` obtains its TDS bearer through `GetCurrentAccessTokenAsync`, then assigns it to `SqlConnection.AccessToken`. The query executor also has a `ServiceClient` for the target environment. Token selection must stay aligned with that client; fixing Dataverse requests while leaving the TDS path dependent on a stale PACX cache is not acceptable.

The SDK documentation says `TokenCacheStorePath` is OAuth-only and must be a full path. Whether the exact SDK version in this repository securely protects the cache on every supported OS, supports silent refresh across processes, and matches a cached identity as expected must be established before relying on it.

## Implementation steps

### ✅ 1. Record the as-is baseline and define the supported authentication matrix

- Run the current build and complete core test suite before changing production code; record failures separately from this work.
- Enumerate the currently supported connection-string profiles and paths: OAuth interactive, Certificate, ClientSecret, project-selected profile, default profile, environment override, and SQL/TDS query.
- Capture current expected behavior for `auth create`, `auth ping`, `auth select`, `auth rename`, `auth delete`, and SQL query, including which flows prompt and when.
- Confirm the supported operating systems for the CLI and the Dataverse Client package version used by both production and test projects.

**Automated tests:** baseline build and full test suite; retain the output as the comparison point. Add no behavior-changing test assumptions before inspecting existing behavior.

**Manual tests:** use an authorized non-production profile without changing the global default. Run `pacx auth ping`, then a read-only SQL/TDS query against `systemuser`; record whether a prompt appears and whether the query succeeds. Do not create or modify Dataverse rows for this baseline.

**Baseline results (2026-10-18):** the Release build passed. The complete core test suite passed with 1,431 tests and 0 failures (`dotnet test test/Greg.Xrm.Command.Core.TestSuite/Greg.Xrm.Command.Core.TestSuite.csproj --configuration Release --no-build`). The repository targets `net10.0`, and the discovered CI workflow runs on Windows; no cross-platform CI matrix or explicit supported-OS list was found. `pacx auth ping --environment rg01` succeeded without changing the default profile. A SQL/TDS point lookup on `systemuser` by primary key returned one row. An unfiltered `SELECT TOP 1 systemuserid FROM systemuser` timed out; this is recorded as an as-is query failure, not evidence of failed TDS authentication. No write operation was performed. The CLI does not report the active profile's authentication type, so this runtime baseline proves the token-based TDS path but does not distinguish OAuth from Certificate or ClientSecret.

**Status:** complete. The source-level matrix includes OAuth, Certificate, and ClientSecret; the runtime baseline covers the currently configured token-based profile only. Windows is the only CI-validated OS; Linux/macOS support remains unspecified and is not claimed by this plan. The broad-query timeout is a known baseline issue to preserve and diagnose separately from token-refresh behavior.

**Exit gate:** passed. Automated baseline, successful TDS authentication, the broad-query timeout, and the limits of the current authentication/OS matrix are documented.

### 2. Prove the Dataverse SDK cache contract before selecting the implementation

- Against the repository's pinned Dataverse Client version, verify that `TokenCacheStorePath` accepts and writes to a full file path, including parent-directory handling.
- Verify OAuth interactive cache lookup across fresh processes and what connection-string identity fields (including `Username`/`OAuthUserId`) are needed to select the intended account.
- Verify silent refresh both after access-token expiry in a still-running `ServiceClient` and after process restart.
- Inspect the actual cache artifact and SDK implementation/package dependencies on each supported OS. Verify encryption at rest, user scoping, file permissions, locking/concurrent-process behavior, and behavior when the cache is unreadable or corrupt. Do not infer encryption merely from MSAL cache support or from a filename/extension.
- Verify `CurrentAccessToken` returns a refreshed token on the SDK-created OAuth `ServiceClient` and is usable for TDS authentication.
- If the SDK cache does not meet the security or refresh requirements, stop and choose an approved secure integration before removing the legacy cache. Do not ship a plaintext SDK cache as a security fix.

**Automated tests:** add focused package-version regression tests for cache path construction and connection-string normalization; add local tests that exercise the SDK cache file with temporary paths where the SDK supports a deterministic non-interactive setup. No test may depend on a developer's real profile or write under the real `%LOCALAPPDATA%`.

**Manual tests:** repeat the authentication in a fresh process with the same test profile; inspect the cache file without printing or logging its contents; test expired-token refresh, concurrent `pacx` processes, and cache-file access denied/corrupt cases on every supported OS.

**Exit gate:** document proven SDK behavior and the chosen cache architecture. If cache encryption is not proven on an OS, either provide a secure supported implementation for that OS or explicitly defer rollout there.

### 3. Add deterministic local tests around token and connection-string decisions

- Add test seams at the smallest boundary needed to test connection-string construction and token selection without Dataverse credentials or network access.
- Make the cache path a full file path under the PACX application-data location. Prefer per-profile cache isolation so deleting or renaming one profile cannot clear or reuse another profile's token. Derive paths safely; never place raw profile names or secrets in filesystem paths.
- Use a connection-string parser/builder rather than substring checks when adding defaults. Preserve user-provided values and case-insensitive key spelling; do not append duplicate `Username` or `TokenCacheStorePath` parameters.
- For OAuth interactive login, persist the authenticated username only when the profile omitted it, and only after successful authentication. Preserve a configured username even if the authenticated account differs; surface that mismatch rather than silently rewriting user intent.
- Keep OAuth-only cache parameters out of Certificate and ClientSecret profiles.
- Preserve per-profile semantics across `auth rename` and `auth delete`: rename the corresponding cache association safely; delete only that profile's native cache, never a shared cache used by other profiles.

**Automated tests:** cover missing/explicit path, file-vs-directory path, duplicate keys, existing username, missing username, usernames containing valid connection-string characters, case-insensitive parameter names, OAuth vs non-OAuth auth types, cache path isolation, profile names requiring path-safe encoding, rename, delete, and two profiles targeting different environments/accounts. Verify failures leave stored connection settings unchanged.

**Manual tests:** create two OAuth profiles for different test identities/environments, use both, rename one, delete one, then verify the other still authenticates and its cache remains usable.

**Exit gate:** local tests prove deterministic configuration and profile isolation without contacting Dataverse.

### 4. Route Dataverse operations through the refresh-capable SDK client

- Make the connection-string/MSAL-backed `ServiceClient` the normal path for OAuth profiles so the SDK owns token acquisition and refresh for the lifetime of a command.
- Remove the static access-token callback path from normal OAuth connection creation only after Step 2 proves the SDK cache can replace its cross-process behavior.
- Retain Certificate and ClientSecret construction paths unchanged except for regression-tested shared connection-string plumbing.
- Handle invalid/missing native cache through the existing supported sign-in behavior. Do not fall back silently to an expired PACX token or another profile.
- Preserve profile selection rules for global default, project profile, named connection, and environment override.

**Automated tests:** verify the selected connection profile and auth mode for each selection path; verify OAuth goes through the refresh-capable factory and non-OAuth profiles retain their existing construction path; verify an authentication failure is surfaced and does not overwrite a valid profile; add a test that invokes the token refresh boundary and confirms subsequent requests use the refreshed value. Run all auth command tests.

**Manual tests:** execute a multi-step Dataverse command that crosses access-token expiry and verify it completes without an anonymous/unauthorized failure or a second interactive prompt. Repeat in a new process to prove persisted silent authentication.

**Exit gate:** Dataverse commands pass both long-running and new-process scenarios on all supported OS/auth modes.

### 5. Make TDS token selection a first-class, same-client contract

- Update `QueryExecutorSql` so the TDS token comes from the authenticated `ServiceClient` used for that query, rather than creating/resolving another connection and potentially obtaining a stale or differently scoped token.
- Retrieve the token through the SDK's refresh-capable token property immediately before opening the SQL connection; do not retain it in a long-lived executor or cache it in PACX.
- Preserve the existing TDS endpoint host/port (`5558`), cancellation propagation, result mapping, and actionable diagnostics for disabled TDS and missing TDS privilege.
- Keep a clear supported way to authenticate with the TDS endpoint. If a token provider abstraction is needed for tests, scope it narrowly and avoid a breaking change to the public plugin interface; assess compatibility before changing `IOrganizationServiceRepository`.
- Ensure the chosen OAuth flow returns a token accepted by TDS. A successful Web API/Organization Service call alone is not proof of TDS authentication.

**Automated tests:** unit-test token selection from the exact supplied client, missing-token failure, environment/profile alignment, and preservation of SQL host/port, cancellation, row mapping, and error translation. Use a local fake at the SQL connection boundary only if needed to keep tests network-free; do not mock away the token-selection contract. Add regression tests for any public interface compatibility decision.

**Manual tests:** run `pacx data query --sql` against a disposable environment with TDS enabled and a user that has TDS permission. Verify a result row is returned. Repeat after access-token expiry while the process/client remains alive and from a fresh process. Confirm disabled-TDS and missing-privilege cases still produce their specific guidance. Test OAuth first; test Certificate/ClientSecret TDS only where those modes are currently supported by the environment.

**Exit gate:** the exact `ServiceClient` used for the command supplies a current bearer token and a real TDS query succeeds before and after refresh.

### 6. Retire the legacy raw-token cache with a safe migration

- Stop writing new raw bearer tokens to `tokens.json`.
- Define cleanup for existing cache entries without treating access tokens as refresh tokens. Never import a legacy bearer into the MSAL cache; it lacks the refresh-token/cache state needed to prove silent renewal.
- Remove an individual legacy entry only after its profile has successfully authenticated through the new path, or provide an explicit one-time cleanup/migration command. Do not delete a shared legacy file before accounting for all profiles.
- Document any unavoidable one-time reauthentication on upgrade. Because a raw access token cannot reconstruct the SDK/MSAL refresh cache, do not claim zero-prompt migration unless a test proves it. If this transition violates the no-regression gate, pause rollout and agree a staged compatibility policy before merging.
- Ensure errors, diagnostics, command results, logs, and test output never expose bearer tokens or cache contents.

**Automated tests:** assert no new `tokens.json` writes; legacy entries are not used to authenticate the new SDK client; cleanup is profile-scoped, idempotent, safe for missing/corrupt files, and occurs only after successful new-path authentication; test concurrent cleanup if multiple processes can update the legacy file.

**Manual tests:** inspect file names, permissions, and metadata only (never print token content); upgrade a disposable installation with existing `tokens.json`, verify the documented migration behavior, use multiple profiles, and confirm cleanup does not affect unrelated profiles.

**Exit gate:** no newly issued plaintext token is persisted, and existing cache cleanup has an explicit user-visible upgrade behavior.

### 7. Run the full regression and release acceptance matrix

- Run focused connection/cache/auth/TDS unit tests, then the complete core test suite and Release build.
- Compare auth command output and stored profile behavior to the Step 1 baseline.
- Run the manual matrix on Windows and every other supported OS: OAuth profile, fresh process, long-running expiry, TDS query, project profile, environment override, two profiles, rename/delete, Certificate, ClientSecret, and cache failure handling.
- Review the diff for public API changes, accidental secret logging, generated artifacts, and unintentional connection-string changes.
- Update user documentation and release notes with cache-file location/behavior, platform security guarantees, migration prompt expectations, and the TDS SQL-query authentication requirements.

**Automated tests:** `dotnet build --configuration Release`; focused test filters; then `dotnet test test/Greg.Xrm.Command.Core.TestSuite/Greg.Xrm.Command.Core.TestSuite.csproj`.

**Manual tests:** complete the matrix above using non-production accounts and record OS, SDK version, auth mode, whether a prompt occurred, and TDS result for each case.

**Definition of done:** all mandatory invariants pass; specifically, an expired token is refreshed for a long-running command and a real SQL/TDS query succeeds using the same authenticated client before and after refresh. Any unresolved platform/security or migration case blocks release rather than being waived as a test gap.
