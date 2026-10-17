# User Role Inspection Alias

## Implementation Plan

### ✅ 1. Command and Delegation

- Add `pacx users getRoles` and all singular/plural, camelCase/kebab-case aliases requested.
- Also expose the equivalent forms under the existing `security` namespace.
- Preserve the optional `--user`/`-u` selector; omission selects the current connected user.
- Delegate execution to `security roles get-by-user`, following the `clear-privilege` forwarding pattern.
- Test all aliases, options, default values, forwarding, unchanged success/failure results, cancellation, and automatic executor registration.

### ✅ 2. Verification

- Run focused command and executor tests and the existing get-by-user tests.
- Build in Release and run the complete local test suite.

Verification: all 24 focused tests pass, including 18 new tests; the solution builds in Release.
The complete suite has 1202 passing tests and the known existing failure in `CheckPrivilegeCommandExecutorTest.ExecuteAsyncOnRecordShouldReturnAccessRightsInResult` (string result cast to a privilege collection). No live Dataverse connection was used.

## Usage

```powershell
pacx users getRoles
pacx user roles -u john.doe@contoso.com
pacx user get-roles --user john.doe@contoso.com
pacx security users get-roles -u john.doe@contoso.com
```

Accepted verbs: `getRoles`, `get-roles`, `getRole`, and `get-role`, with either `users` or `user`, optionally prefixed with `security`.
The additional alias `pacx user roles` is also supported.
The user selector accepts a system user GUID, domain name, or primary email.
The behavior and output are identical to `pacx security roles getByUser`: direct roles and roles inherited through teams are listed with their assignment sources.
Microsoft Entra group-team membership is subject to Dataverse synchronization, as in the original command.
