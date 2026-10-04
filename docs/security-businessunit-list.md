# Security Business Unit Hierarchy

## Implementation Plan

### ✅ 1. Retrieve Business Units

- Extend the existing `BusinessUnit` model with its parent lookup.
- Add `GetAllAsync` to `IBusinessUnitRepository` and retrieve every page.
- Test selected columns, parent mapping, paging, and cancellation locally.

### ✅ 2. List the Hierarchy

- Add `pacx security businessunit list`, with `security bu list` and `security bu tree` aliases.
- Support `--format Tree|Json` (`-f`), defaulting to `Tree`.
- Build the hierarchy using parent IDs, sorting siblings by name and then ID.
- Keep nodes whose parents are not returned at root level; reject cycles explicitly.
- Render escaped names and IDs through `IAnsiConsole`, or a nested JSON object through `IOutput` without progress output.
- Test parsing, hierarchy, output, failures, interface-based IoC resolution, and cancellation.

### ✅ 3. Verify and Document

- Run the focused tests and full suite; report unrelated existing failures.
- Document usage and the JSON structure.

## Usage

```powershell
pacx security businessunit list
pacx security bu tree
pacx security bu list --format Json --nologo
```

The tree starts after a blank line and follows the interactive palette: static labels are SkyBlue2, business unit names SandyBrown, and identifiers/counts Gray. JSON output remains uncolored and has no extra blank line.

JSON is an object with a `BusinessUnits` array containing root nodes. Each node contains `Id`, `Name`, `ParentId`, and a recursively nested `Children` array. Empty environments return `{ "BusinessUnits": [] }`.

```json
{
	"BusinessUnits": [
		{
			"Id": "00000000-0000-0000-0000-000000000001",
			"Name": "Contoso",
			"ParentId": null,
			"Children": [
				{
					"Id": "00000000-0000-0000-0000-000000000002",
					"Name": "Europe",
					"ParentId": "00000000-0000-0000-0000-000000000001",
					"Children": []
				}
			]
		}
	]
}
```

The command reads all pages without filtering out disabled business units. Root nodes and siblings are sorted by name (case-insensitive), then ID. Missing parents do not hide their children; cyclic parent relationships fail before any JSON is written.

## Verification

- All 22 command/repository tests passed locally without Dataverse access.
- Latest full suite: all 1,364 tests pass, including the corrected cancellation-before-connection tests for user profiles and team roles. The six duplicate service copies under `Commands/Security` have been removed; compilation and tests pass without source exclusions.
- No live-environment execution was performed.
