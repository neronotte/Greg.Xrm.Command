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

### ✅ 2. Prove the Dataverse SDK cache contract before selecting the implementation

- Against the repository's pinned Dataverse Client version, verify that `TokenCacheStorePath` accepts and writes to a full file path, including parent-directory handling.
- Verify OAuth interactive cache lookup across fresh processes and what connection-string identity fields (including `Username`/`OAuthUserId`) are needed to select the intended account.
- Verify silent refresh both after access-token expiry in a still-running `ServiceClient` and after process restart.
- Inspect the actual cache artifact and SDK implementation/package dependencies on each supported OS. Verify encryption at rest, user scoping, file permissions, locking/concurrent-process behavior, and behavior when the cache is unreadable or corrupt. Do not infer encryption merely from MSAL cache support or from a filename/extension.
- Verify `CurrentAccessToken` returns a refreshed token on the SDK-created OAuth `ServiceClient` and is usable for TDS authentication.
- If the SDK cache does not meet the security or refresh requirements, stop and choose an approved secure integration before removing the legacy cache. Do not ship a plaintext SDK cache as a security fix.

**Automated tests:** add focused package-version regression tests for cache path construction and connection-string normalization; add local tests that exercise the SDK cache file with temporary paths where the SDK supports a deterministic non-interactive setup. No test may depend on a developer's real profile or write under the real `%LOCALAPPDATA%`.

**Manual tests:** repeat the authentication in a fresh process with the same test profile; inspect the cache file without printing or logging its contents; test expired-token refresh, concurrent `pacx` processes, and cache-file access denied/corrupt cases on every supported OS.

**Evidence so far (2026-10-18):** production and test projects pin `Microsoft.PowerPlatform.Dataverse.Client` 1.2.26, with `Microsoft.Identity.Client` and `Microsoft.Identity.Client.Extensions.Msal` 4.84.2. The pinned package XML describes `TokenCacheStorePath` as a path and file name. The SDK source inspected uses `MsalCacheHelper`, registers the cache with the public-client user's MSAL cache, splits the supplied value into a file name and directory, configures Linux keyring and macOS Keychain integration, and uses a case-insensitive username hint to find an account for `AcquireTokenSilent`. This is source evidence; the inspected source revision is not yet proven to be the exact source revision for package 1.2.26, and the on-disk protection of that exact package has not yet been validated.

Creating the explicitly authorized temporary OAuth profile through the current PACX executable produced a 2,781-byte `tokens.json`; no token contents were read. No separate MSAL cache file appeared in PACX's configured cache directory or its parent directory. The temporary profile was deleted, and the legacy cache file returned to 2 bytes. An earlier cleanup check reported `generali-dev` as the global default; a later read-only `pacx auth list` now reports `rulmeca-preventivi` and an additional profile not present in the initial inventory. No profile selection was changed after noticing this discrepancy; treat the current profile/default state as user-owned and restore only with explicit confirmation. The terminal showed no interactive sign-in prompt, so this does not prove a fresh interactive login. This confirms the current plaintext-cache risk and means further OAuth runtime tests must use a path that does not persist PACX bearer tokens.

**Status:** complete. All runtime verifications are complete on Windows and Linux (below) and the cache architecture has been approved by the maintainer.

**Isolated SDK harness results (2026-10-18, Windows, rg01, OAuth interactive user):** a throwaway console harness outside the repository (same `Dataverse.Client` 1.2.26 and `Microsoft.Data.SqlClient` 7.0.2) created `ServiceClient` from `AuthType=OAuth;Url=…;RedirectUri=http://localhost;LoginPrompt=Auto;TokenCacheStorePath=<temp>\cache\nested\msal.cache`. No token or cache content was printed.

- Full file path: accepted; the missing nested parent directories were created.
- Encryption at rest (Windows): the cache file is not JSON, contains no plaintext `AccessToken`/`RefreshToken` markers, and DPAPI `CurrentUser` unprotects it. Linux keyring and macOS Keychain remain unverified.
- First login: interactive (completed in the browser); `WhoAmI` and a TDS point query using `ServiceClient.CurrentAccessToken` succeeded.
- Cross-process reuse with `LoginPrompt=Auto` + `Username=<authenticated UPN>`: a new process obtained the **same** cached token (identical expiry), connected in ~1 s, and `WhoAmI` + TDS succeeded. Reuse proven.
- Without `Username`: the SDK did not find the cached account and fell back to interactive acquisition (new token expiry, ~71 s, satisfied silently only by browser SSO). **`Username` is required** for cache reuse; PACX must persist the authenticated UPN in OAuth profiles that omit it.
- `LoginPrompt=Never`: in SDK 1.2.26 this calls `AcquireTokenInteractive` with `NoPrompt` and never reads the cache (each run returned a new token). PACX must keep `LoginPrompt=Auto` for OAuth profiles.
- The OAuth user (`3586c4d5…`) differs from the user of the existing `rg01` PACX profile (`40595c8f…`), so that profile is likely not OAuth interactive; the Step 1 TDS baseline therefore covered a non-interactive auth type.
- Long-running refresh (proven): a soak on a single `ServiceClient` ran `WhoAmI` + TDS every 5 minutes from 14:57Z to 16:27Z. All 19 probes succeeded. The token expiring at 16:10:11Z was refreshed by the SDK, and the 16:12:17Z probe used a new token (expiry 17:19:22Z) without any prompt. `CurrentAccessToken` returned the refreshed token and TDS accepted it in the 4 probes after the refresh. The soak was stopped after these post-refresh confirmations.
- Refresh after process restart (proven): at 17:20Z the cached access token had expired (17:19:22Z). New processes silently renewed it from the cached refresh token (no browser, ~2 s) and `WhoAmI` + TDS succeeded.
- Concurrent processes (proven): 3 processes started in parallel on the same cache all succeeded (`WhoAmI` + TDS). Two refreshed concurrently and obtained different tokens, which is harmless; the cache file remained valid and DPAPI-protected.
- Corrupt cache (proven): a separate cache file filled with 4 KB of random bytes caused no error. The SDK ignored it, re-acquired interactively (~15 s, completed by browser SSO) and overwrote it with a valid DPAPI-protected cache.
- Inaccessible cache (proven): a copy of the cache with read/write denied to the current user caused no SDK error. The SDK re-acquired interactively, `WhoAmI` + TDS succeeded, and nothing could be persisted. (The harness's own post-run file inspection threw `UnauthorizedAccessException`; this is the test tool, not the SDK.) The ACL was restored afterwards.
- These runtime results were obtained with the exact pinned package binaries, which resolves the earlier caveat about the inspected source revision.

**OS scope decision (2026-10-18, approved by the maintainer):** this change is validated and declared supported on Windows only, matching the Windows-only CI. Linux keyring and macOS Keychain cache protection are not verified and are out of the declared scope; "every supported OS" in later steps means Windows.

**Linux results (2026-10-18, WSL 2, Ubuntu 26.04.1, .NET SDK 10.0.402, libsecret + gnome-keyring, same harness and packages):** the maintainer then provided WSL to extend verification. macOS remains unverified.

- Keyring available and unlocked: first OAuth login, `WhoAmI` and TDS succeeded from Linux. The `TokenCacheStorePath` file is created empty (0 bytes); the MSAL cache is stored in the keyring (item labelled `Token Storage for <process name>`). Cross-process reuse with `Username` proven (same token expiry, no browser).
- Desktop session with the keyring daemon stopped: D-Bus auto-activates `gnome-keyring`; if the keyring is locked, the user is prompted to unlock it. When unlocked, behaviour equals the previous case.
- Keyring reachable but locked and not unlocked: no error, no cache persisted, nothing written to disk.
- Headless (no reachable D-Bus/keyring): no error, `WhoAmI` and TDS succeed, **no cache file and no token written anywhere**. The cache is therefore in-memory only: every new process needs a fresh interactive login.
- A scan of files modified in the user's home during the tests found no `RefreshToken` marker; only the encrypted keyring files and empty cache files were touched.

**Finding to resolve in Steps 3-6 (possible regression on headless Linux):** today PACX reuses the raw `tokens.json` bearer for later processes even without a keyring. With the SDK cache only, OAuth interactive users on headless Linux would be prompted on every command (and fail without a browser). The design must make this explicit, e.g. detect persistence unavailability (`MsalCacheHelper.VerifyPersistence`) and warn, and recommend ClientSecret/Certificate profiles for headless automation. It must not fall back to plaintext token storage.

**Chosen cache architecture (approved by the maintainer, 2026-10-18):**

1. **Capability-based selection, not OS-based.** At runtime PACX checks whether the OS secure storage is usable (`MsalCacheHelper.VerifyPersistence`: DPAPI on Windows, keyring on Linux, Keychain on macOS). The check uses no tokens.
2. **Secure storage available** (Windows; Linux with a usable keyring): OAuth profiles use the SDK MSAL cache (`TokenCacheStorePath` = full file path, `LoginPrompt=Auto`, `Username` = authenticated UPN). PACX no longer writes `tokens.json` for them. The SDK refreshes tokens within a command and across processes.
3. **Secure storage unavailable** (headless Linux, WSL without keyring, locked/refused keyring): keep the as-is legacy `tokens.json` behaviour, so there is no regression. Mitigations: file restricted to the owner (`0600`) on Unix, an explicit warning when the profile is created, and a recommendation to use ClientSecret/Certificate profiles for headless automation. In this mode the issue 217 limitation (no refresh beyond the cached access token) remains and is documented. A plaintext MSAL cache (`WithUnprotectedFile`) is rejected: it would persist a long-lived refresh token unencrypted, and the SDK does not expose it anyway.
4. **One shared MSAL cache file for all profiles**, instead of one per profile. Reasons: on Linux the keyring entry is keyed by fixed SDK attributes, not by file name, so per-profile files would share one entry anyway; MSAL natively stores multiple accounts and `Username` selects the right one; profiles of the same tenant share one sign-in. Consequently `auth delete` and `auth rename` do not modify the MSAL cache (the account can still be used by other profiles); they keep handling the legacy `tokens.json` entry as today.
5. **Non-interactive profiles (ClientSecret, Certificate) are unchanged**: the SDK already refreshes confidential-client tokens in memory, and their connection path stays byte-for-byte identical to today (including the directory `TokenCacheStorePath` that the SDK ignores for them).
6. **Comments:** the maintainer requires the implementation to document the reasons for each of these decisions in code comments.

**Exit gate:** passed. SDK behaviour is proven on Windows and Linux (keyring and headless), and the cache architecture is chosen and approved. macOS is not verified and not claimed.

### ✅ 3. Add deterministic local tests around token and connection-string decisions

- Add test seams at the smallest boundary needed to test connection-string construction and token selection without Dataverse credentials or network access.
- Make the cache path a full file path under the PACX application-data location: one shared MSAL cache file (see the chosen architecture in Step 2; per-profile files would not isolate anything on Linux).
- Use a connection-string parser/builder rather than substring checks when adding defaults. Preserve user-provided values and case-insensitive key spelling; do not append duplicate `Username` or `TokenCacheStorePath` parameters.
- For OAuth interactive login, persist the authenticated username only when the profile omitted it, and only after successful authentication. Preserve a configured username even if the authenticated account differs; surface that mismatch rather than silently rewriting user intent.
- Keep the connection path of Certificate and ClientSecret profiles byte-for-byte unchanged. They still receive the legacy directory `TokenCacheStorePath`, which the SDK ignores for confidential clients; leaving it avoids any behavioural difference for them.
- Preserve as-is semantics of `auth rename` and `auth delete`: they keep handling the legacy `tokens.json` entry and never clear the shared MSAL cache.
- Add the capability-based mode selection (secure MSAL cache vs legacy fallback) as a pure, testable decision, with the secure-storage probe behind an interface so tests do not touch the real OS storage.

**Automated tests:** cover missing/explicit path, file-vs-directory path, duplicate keys, existing username (all aliases), missing username, values requiring quoting, case-insensitive parameter names, OAuth vs non-OAuth auth types, mode selection for every auth type and storage availability, and the username persistence rules (added only when missing, only for OAuth, never overwriting, stored settings unchanged on failure).

**Manual tests:** none required for this step; it is covered end-to-end by the manual tests of Steps 4-7 (two OAuth profiles, rename, delete, cache reuse).

**Results (2026-10-18):** added `OAuthConnectionString` (append-only `TokenCacheStorePath`/`Username` rules), `TokenCacheMode` + `TokenCacheModeSelector` (capability-based, probe evaluated only for OAuth), `ISecureTokenStorageProbe` + `MsalSecureTokenStorageProbe` (`VerifyPersistence`, lazy, once per process) and `TokenCache.GetMsalCacheFilePath`. No runtime path uses them yet (wired in Step 4). 46 new local tests pass; full suite 1,477 passed, 0 failed (baseline 1,431 + 46). Username-persistence tests on the repository are added in Step 4, where that logic lives.

**Status:** complete.

**Exit gate:** passed. Local tests prove deterministic configuration and mode selection without contacting Dataverse.

### ✅ 4. Route Dataverse operations through the refresh-capable SDK client

- Make the connection-string/MSAL-backed `ServiceClient` the normal path for OAuth profiles when secure storage is available, so the SDK owns token acquisition and refresh for the lifetime of a command.
- Keep the static access-token callback path only for the legacy fallback (secure storage unavailable), unchanged from today.
- Persist the authenticated `Username` into OAuth profiles that omit it, both at `auth create` and on the first successful connection of existing profiles (one-time interactive sign-in after upgrade).
- Retain Certificate and ClientSecret construction paths unchanged except for regression-tested shared connection-string plumbing.
- Handle invalid/missing native cache through the existing supported sign-in behavior. Do not fall back silently to an expired PACX token or another profile.
- Preserve profile selection rules for global default, project profile, named connection, and environment override.

**Automated tests:** verify the selected connection profile and auth mode for each selection path; verify OAuth goes through the refresh-capable factory and non-OAuth profiles retain their existing construction path; verify an authentication failure is surfaced and does not overwrite a valid profile; add a test that invokes the token refresh boundary and confirms subsequent requests use the refreshed value. Run all auth command tests.

**Manual tests:** execute a multi-step Dataverse command that crosses access-token expiry and verify it completes without an anonymous/unauthorized failure or a second interactive prompt. Repeat in a new process to prove persisted silent authentication.

**Exit gate:** Dataverse commands pass both long-running and new-process scenarios on all supported OS/auth modes.

**Results so far (2026-10-18, Windows, rg01, PACX built from this branch):** full suite 1,486 passed (9 new repository username tests).

- A. `auth create` of a new OAuth profile: one browser sign-in; `msal_token_cache.bin` created (8,662 bytes, DPAPI); `tokens.json` not written.
- B. New processes on that profile: `auth ping` (4 s) and a TDS query (3 s) reused the cache with no browser, which also proves `Username` was stored.
- C. Upgrade of a profile created by the current release (`master` build, which wrote a 2,721-byte plaintext token to `tokens.json`): first branch command signed in once (9 s, satisfied by browser SSO) and stored `Username`; the next command was silent (3 s).
- D. `auth rename`: the renamed profile stayed silent (3 s). `auth delete`: the other profile kept working silently (2 s); the shared MSAL cache was not touched.
- E. Pre-existing non-OAuth `rg01` profile: `auth ping` and a TDS query behave as in the Step 1 baseline.
- Long-running command (passed): a temporary plugin (outside the repository, loaded with `--tool`) held ONE connection from `GetCurrentConnectionAsync` and ran `WhoAmI` + TDS through the real `QueryExecutorSql` every 5 minutes from 18:16Z to 19:41Z. All 18 probes succeeded. The token expiring at 19:34:39Z was refreshed by the SDK without any prompt: the 19:36:53Z probe used a new token (expiry 20:56:58Z), and both post-refresh probes succeeded.

**Status:** complete (Windows, the declared scope). The legacy `tokens.json` entry of an upgraded profile is still present: its cleanup belongs to Step 6.

**Exit gate:** passed. Long-running and new-process scenarios pass for OAuth (SDK cache) and the non-OAuth profile is unchanged.

### ✅ 5. Make TDS token selection a first-class, same-client contract

- Update `QueryExecutorSql` so the TDS token comes from the authenticated `ServiceClient` used for that query, rather than creating/resolving another connection and potentially obtaining a stale or differently scoped token.
- Retrieve the token through the SDK's refresh-capable token property immediately before opening the SQL connection; do not retain it in a long-lived executor or cache it in PACX.
- Preserve the existing TDS endpoint host/port (`5558`), cancellation propagation, result mapping, and actionable diagnostics for disabled TDS and missing TDS privilege.
- Keep a clear supported way to authenticate with the TDS endpoint. If a token provider abstraction is needed for tests, scope it narrowly and avoid a breaking change to the public plugin interface; assess compatibility before changing `IOrganizationServiceRepository`.
- Ensure the chosen OAuth flow returns a token accepted by TDS. A successful Web API/Organization Service call alone is not proof of TDS authentication.

**Automated tests:** unit-test token selection from the exact supplied client, missing-token failure, environment/profile alignment, and preservation of SQL host/port, cancellation, row mapping, and error translation. Use a local fake at the SQL connection boundary only if needed to keep tests network-free; do not mock away the token-selection contract. Add regression tests for any public interface compatibility decision.

**Manual tests:** run `pacx data query --sql` against a disposable environment with TDS enabled and a user that has TDS permission. Verify a result row is returned. Repeat after access-token expiry while the process/client remains alive and from a fresh process. Confirm disabled-TDS and missing-privilege cases still produce their specific guidance. Test OAuth first; test Certificate/ClientSecret TDS only where those modes are currently supported by the environment.

**Exit gate:** the exact `ServiceClient` used for the command supplies a current bearer token and a real TDS query succeeds before and after refresh.

**Results so far (2026-10-18):** `QueryExecutorSql` now takes the token from the `ServiceClient` running the query (`ResolveAccessTokenAsync`), falling back to the unchanged repository token only when the client has none (fixed-token legacy clients). 5 new tests; full suite 1,491 passed. TDS queries succeeded through PACX in manual tests B, E and in all 18 probes of the long-running test of Step 4, including the 2 probes after the token refresh (19:36Z, 19:41Z), using the refreshed token of the same client. Legacy-mode TDS is validated in Step 6.

**Status:** complete (Windows, the declared scope).

**Exit gate:** passed. The `ServiceClient` running the command supplies a current token, and a real TDS query succeeds before and after the refresh.

### ✅ 6. Retire the legacy raw-token cache with a safe migration

- Stop writing new raw bearer tokens to `tokens.json` when secure storage is available; keep it only for the approved legacy fallback, restricted to the owner (`0600`) on Unix and announced by a warning at profile creation.
- Define cleanup for existing cache entries without treating access tokens as refresh tokens. Never import a legacy bearer into the MSAL cache; it lacks the refresh-token/cache state needed to prove silent renewal.
- Remove an individual legacy entry only after its profile has successfully authenticated through the new path, or provide an explicit one-time cleanup/migration command. Do not delete a shared legacy file before accounting for all profiles.
- Document any unavoidable one-time reauthentication on upgrade. Because a raw access token cannot reconstruct the SDK/MSAL refresh cache, do not claim zero-prompt migration unless a test proves it. If this transition violates the no-regression gate, pause rollout and agree a staged compatibility policy before merging.
- Ensure errors, diagnostics, command results, logs, and test output never expose bearer tokens or cache contents.

**Automated tests:** assert no new `tokens.json` writes; legacy entries are not used to authenticate the new SDK client; cleanup is profile-scoped, idempotent, safe for missing/corrupt files, and occurs only after successful new-path authentication; test concurrent cleanup if multiple processes can update the legacy file.

**Manual tests:** inspect file names, permissions, and metadata only (never print token content); upgrade a disposable installation with existing `tokens.json`, verify the documented migration behavior, use multiple profiles, and confirm cleanup does not affect unrelated profiles.

**Exit gate:** no newly issued plaintext token is persisted, and existing cache cleanup has an explicit user-visible upgrade behavior.

**Results (2026-10-18):**

- Code: `TokenCache` writes `tokens.json` owner-only (`0600`, applied before writing, also to files created by older versions) on Unix and unchanged on Windows; internal directory-based overloads make it testable on a temporary folder. `OrganizationServiceRepository` retires the profile's `tokens.json` entry after each successful SDK-cache connection (best effort: locked/corrupt/unreadable files never fail the connection) and `auth create` warns when the profile falls into legacy mode. Legacy tokens are never imported into the MSAL cache.
- Automated: 13 new tests (round-trip, profile-scoped and idempotent removal, no file creation/rewrite when nothing to remove, corrupt and locked files, owner-only permissions on new and pre-existing files, warning shown only for OAuth without secure storage and never probing for non-OAuth). Full suite 1,504 passed on Windows; the connection/cache tests (59) also pass on Linux (WSL), where the `0600` test is effective.
- Manual, Windows: the upgraded `issue217-old` profile (2,721-byte plaintext entry left by the `master` build) connected through the SDK cache with no prompt and `tokens.json` dropped to 2 bytes; the non-OAuth `rg01` profile is unchanged.
- Manual, headless Linux (WSL, no reachable D-Bus/keyring, isolated PACX data folder): `auth create` showed the legacy warning; `tokens.json` was created with mode `600`; in new processes `auth ping` (2 s) and a TDS query (3 s) reused the stored token with no browser, exactly as before #217.
- Diagnostic note: three earlier Linux attempts timed out only because nobody completed the browser sign-in. A first diagnosis that suggested a hang before the browser launch was wrong (in WSL `xdg-open` opens the Windows browser through `explorer.exe` and ignores `$BROWSER`); a corrected diagnosis showed PACX launches the browser within 1 s.

**Status:** complete.

**Exit gate:** passed. No new plaintext token is persisted where secure storage exists; legacy entries are removed after the first successful SDK-cache connection; the legacy fallback is restricted to the owner and announced to the user.

### ✅ 7. Run the full regression and release acceptance matrix

- Run focused connection/cache/auth/TDS unit tests, then the complete core test suite and Release build.
- Compare auth command output and stored profile behavior to the Step 1 baseline.
- Run the manual matrix on Windows and every other supported OS: OAuth profile, fresh process, long-running expiry, TDS query, project profile, environment override, two profiles, rename/delete, Certificate, ClientSecret, and cache failure handling.
- Review the diff for public API changes, accidental secret logging, generated artifacts, and unintentional connection-string changes.
- Update user documentation and release notes with cache-file location/behavior, platform security guarantees, migration prompt expectations, and the TDS SQL-query authentication requirements.

**Automated tests:** `dotnet build --configuration Release`; focused test filters; then `dotnet test test/Greg.Xrm.Command.Core.TestSuite/Greg.Xrm.Command.Core.TestSuite.csproj`.

**Manual tests:** complete the matrix above using non-production accounts and record OS, SDK version, auth mode, whether a prompt occurred, and TDS result for each case.

**Definition of done:** all mandatory invariants pass; specifically, an expired token is refreshed for a long-running command and a real SQL/TDS query succeeds using the same authenticated client before and after refresh. Any unresolved platform/security or migration case blocks release rather than being waived as a test gap.

**Results (2026-10-18):**

- Builds: the whole solution builds in Release and Debug with 0 errors. All 60 warnings are pre-existing NU190x package-vulnerability warnings; no package was changed by this work.
- Tests: full suite 1,504 passed, 0 failed (baseline 1,431 + 73 new). Connection/cache tests also pass on Linux.
- Manual matrix (Windows, rg01): OAuth new profile, fresh process, upgrade from the current release, rename, delete, long-running refresh with TDS before/after refresh (Steps 4-5), legacy cleanup (Step 6). Non-OAuth: the `rg01` profile is ClientSecret; ping and TDS unchanged. No Certificate profile was available: Certificate follows the same unchanged code path as ClientSecret, covered by unit tests on the mode selection.
- Manual matrix (Linux, WSL): SDK cache with keyring (Step 2) and legacy fallback without keyring through PACX (Step 6). macOS not verified and not claimed.
- Public API: `Greg.Xrm.Command.Interfaces` (plugin contract) is unchanged. `GetTokenCachePath()` keeps returning the directory. The repository constructor gained an optional parameter, so DI registration and existing callers are unchanged.
- Secrets: new output only shows profile names, user names, file paths and SDK error messages; never tokens or connection strings.
- Documentation: `auth create` usage (exported to the wiki page `pacx-auth-create.md`) gains a "How tokens are stored (OAuth profiles)" section describing the encrypted cache, automatic refresh, the stored `Username`, the one-time sign-in after upgrade, TDS, and the headless fallback. Every design decision is documented in code comments, as requested by the maintainer.

**Out of scope, found during the work (to track separately):**

1. On Linux, if `~/.local/share` does not exist, PACX stops at startup with "The value cannot be an empty string. (Parameter 'path')" (`Storage.cs` and `TokenCache.cs` use `GetFolderPath(LocalApplicationData)`). Pre-existing.
2. `System.Security.Cryptography.Pkcs` 10.0.10 has known moderate and high vulnerabilities (NU1902/NU1903). Pre-existing.

**Status:** complete.

**Exit gate:** passed.
