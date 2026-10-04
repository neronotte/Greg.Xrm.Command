# Security Business Unit Hierarchy

## Implementation Plan

### ✅ 1. Retrieve Business Units

- Extend the existing `BusinessUnit` model with its parent lookup.
- Add `GetAllAsync` to `IBusinessUnitRepository` and retrieve every page.
- Test selected columns, parent mapping, paging, and cancellation locally.

### 🕒 2. List the Hierarchy

- Add `pacx security businessunit list`, with `security bu list` and `security bu tree` aliases.
- Support `--format Tree|Json` (`-f`), defaulting to `Tree`.
- Build the hierarchy using parent IDs, sorting siblings by name and then ID.
- Keep nodes whose parents are not returned at root level; reject cycles explicitly.
- Render escaped names and IDs through `IAnsiConsole`, or a nested JSON object through `IOutput` without progress output.
- Test parsing, hierarchy, output, failures, interface-based IoC resolution, and cancellation.

### 3. Verify and Document

- Run the focused tests and full suite; report unrelated existing failures.
- Document usage and the JSON structure.

## Usage

```powershell
pacx security businessunit list
pacx security bu tree
pacx security bu list --format Json --nologo
```

JSON is an object with a `BusinessUnits` array containing root nodes. Each node contains `Id`, `Name`, `ParentId`, and a recursively nested `Children` array. Empty environments return `{ "BusinessUnits": [] }`.