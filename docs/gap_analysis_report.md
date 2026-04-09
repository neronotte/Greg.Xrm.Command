# Gap Analysis: Power Platform Automation (PAC CLI vs PACX)

## Overview
This report identifies the gaps in automation capabilities between the official Power Platform CLI (PAC) and the community-driven PACX, with a focus on areas where PACX can be extended to provide feature parity with advanced management and debugging tools.

## Comparison Matrix

| Feature Area | PAC CLI Capability | PACX Current Status | Target Capability Gap |
| :--- | :--- | :--- | :--- |
| **Environment Discovery** | `pac org list`, `pac admin list` | `auth list`, `org list` | Detailed SKU/Region info, connection listing. |
| **Flow Management** | `pac canvas list`, `pac solution add-instance` | `workflow list` | Get definition (JSON), update definition, set state (Start/Stop). |
| **Flow Debugging** | Limited (telemetry) | None | Run history, detailed error breakdown per action, input/output inspection. |
| **Flow Execution** | None | None | Trigger HTTP flows, resubmit runs, cancel runs. |
| **Solution Management**| `pac solution init/import/export` | `solution list/create/delete` | Add/Remove components individually, manage constants. |
| **Unified Routing** | None | `unifiedrouting list/status` | Full parity with UR management APIs. |
| **Dataverse Schema** | `pac data clone` | `column`, `table`, `rel` (extensive) | Advanced metadata manipulation, Mermaid diagram generation. |

## Key Findings

1. **Deep Debugging Gap:** Official PAC CLI and current PACX lack the ability to inspect individual Flow run actions, inputs, and outputs. This is a major productivity blocker for developers.
2. **Execution Control Gap:** There is no command-line way to resubmit or cancel Flow runs, or to start/stop Flows without updating the whole solution.
3. **Connection Visibility:** Neither tool provides a simple way to list and check the status of all connections within an environment via CLI.
4. **Parity Goal:** To achieve parity with advanced automation sets, PACX should expand its `workflow` domain to include run management and inspection.

## Recommendations for New PACX Commands

### 1. Workflow Run Management
- `pacx workflow run list`: List recent runs with status and timestamps.
- `pacx workflow run get`: Get detailed error info and action outputs for a specific run.
- `pacx workflow run resubmit`: Re-run a failed Flow.
- `pacx workflow run cancel`: Cancel an active run.

### 2. Workflow Definition & State
- `pacx workflow get`: Download the full JSON definition of a Flow.
- `pacx workflow set-state`: Start or stop a Flow.

### 3. Environment & Connections
- `pacx connection list`: List all connections in the current environment with their status.

## Next Steps
Incorporate these recommendations into the **Extend Automation Capabilities (as Plugin)** track.
