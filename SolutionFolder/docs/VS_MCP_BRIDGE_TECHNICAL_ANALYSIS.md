# VS MCP Bridge Technical Analysis

Last updated: 2026-07-19

## Purpose

This document is the living technical reference for `vs-mcp-bridge`.

Its job is to stay grounded in the repository as it exists now, not in an aspirational future architecture.

For fast onboarding, start with `AI_START.md`, then continue with `README.md`, `SolutionFolder/docs/ARCHITECTURE.md`, and `SolutionFolder/docs/gated_turn-based_workflow-Codex.txt`.

## Executive Summary

`vs-mcp-bridge` is a local bridge between an MCP client and a host that can expose IDE or workspace state.

Current host shape:

- `VsMcpBridge.McpServer` is the MCP stdio process
- `VsMcpBridge.Vsix` is the Visual Studio host
- `VsMcpBridge.App` is a standalone WPF host for shared bridge behavior
- `VsMcpBridge.Shared` and `VsMcpBridge.Shared.Wpf` hold the common contracts and UI pieces

The project is in early design. Basic infrastructure exists (the host shape
above), but no functionality is committed, working, or supported yet — see
[current-bridge-capabilities.md](current-bridge-capabilities.md). Work ahead
follows architectural design, then a gap analysis, then a prioritized
backlog, then sprints; nothing here should be read as a validated baseline.

## Solution Layout

### `VsMcpBridge.McpServer`

Purpose:

- hosts the MCP stdio server
- defines MCP tools
- forwards typed requests to the active host over a named pipe

Key files:

- `Program.cs`
- `Pipe/PipeClient.cs`
- `Tools/VsTools.cs`

### `VsMcpBridge.Vsix`

Purpose:

- hosts the bridge inside Visual Studio
- starts the named-pipe listener
- handles Visual Studio-backed requests
- owns the tool window shell
- now also hosts prompt-box chat request handling through a VSIX-side `IChatRequestService` implementation for parity with the standalone app

Key files:

- `VsMcpBridgePackage.cs`
- `Pipe/PipeServer.cs`
- `Services/VsService.cs`
- `ToolWindows/LogToolWindow.cs`
- `VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml`

### `VsMcpBridge.App`

Purpose:

- exercises shared bridge pieces outside the VSIX host
- serves as a secondary host example

Current phase note:

- this project's role (reference host and separation check) is design intent; nothing here is built or working functionality yet

### `VsMcpBridge.Shared`

Purpose:

- request and response models
- shared abstractions and services
- shared diagnostics and MVP/VM support

### `VsMcpBridge.Shared.Wpf`

Purpose:

- shared WPF views that can be hosted outside the VSIX assembly

## Runtime Architecture

```text
AI client
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON line messages over named pipe "VsMcpBridge"
host
  -> Visual Studio APIs             (VSIX)
  -> workspace/file-system services (App)
```

## Current Constraints

The following constraints are intentional and should be preserved unless Bill decides otherwise:

- no shell execution from the MCP server
- no direct workspace or solution file writes from the MCP server
- no general filesystem access outside the solution root, except for bridge diagnostics written under `%LocalAppData%\VsMcpBridge\Logs`
- no autonomous agent loop
- edits remain proposal-based rather than silently applied
- Visual Studio API access stays in the VSIX host

## Current Stage

Early design — see [current-bridge-capabilities.md](current-bridge-capabilities.md).
Nothing in this repository is verified, working, or supported functionality;
treat every item below as design direction, not delivered behavior.

Logging design direction:

- `Trace` should describe verbose class/process flow across the decoupled bridge so the runtime remains diagnosable during rapid triage
- `Information` is the default user-facing level and should carry non-verbose operational feedback
- avoid redundant paired Trace and Information documentation or logging for the same event
- `StdErr` is the preferred transport-safe out-of-band channel for diagnostics that must not pollute MCP stdio traffic
- when Trace is enabled, Trace-level output should also surface through the shared UI log view so it is visible to the operator and AI tools during investigation
- the shared logging pipeline should include an abstraction seam that can forward trace and possibly information events to additional persistence targets such as SQL, starting with a file-backed forwarder

## Current Technical Priorities

The next phase is design, not implementation hardening.

Priority order:

1. complete architectural design
2. run a gap analysis against that design
3. prioritize the resulting backlog
4. execute sprints against the prioritized backlog, committing only to what each sprint delivers

## Known Risk Areas

1. There is no automated regression coverage for the MCP and pipe boundaries yet.
2. Edit application still rebuilds the full document and may need hardening around formatting and line endings.
3. Diagnostics must remain transport-safe for MCP host work so stdio JSON transport stays clean; StdErr and file-backed sinks are acceptable, stdout is not.
4. Non-blocking `NotificationReceived` JsonRpc warnings were observed after apply and may deserve investigation only if they become user-visible or disruptive.
5. Increased log volume at `Trace` can reduce signal if left enabled continuously; operators should use diagnostic levels during focused investigation windows.

## Operational Note: Tool Window Readiness

During manual testing, the VS MCP Bridge tool window could appear visually ready before startup-related processing and log activity had fully settled.

Observed guidance:

- do not assume the tool window is fully ready the moment it becomes visible
- when validating interactive behavior, wait until startup log activity has stopped before treating the UI as ready for clicks and text interaction

This is currently preserved as an operational note rather than a confirmed defect.

If a future developer or AI agent sees transient dead-click behavior early in startup, first verify whether the interaction happened before the tool window had actually finished settling before opening a broader thread-lock investigation.

## Guidance For Future Changes

During the current design phase:

- do not add capabilities before the architectural design, gap analysis, and backlog prioritization that are supposed to justify them
- keep docs aligned with actual reality — no capability claim belongs in this document until a sprint has delivered and validated it
- prefer small, reviewable design decisions over speculative scope

Once sprints are underway, likely next technical topics include automated
tests around MCP startup and pipe flow, clearer protocol/version metadata,
structured edit models beneath the diff preview, and the approval/apply
workflow — but which of these matter, and in what order, is exactly what the
gap analysis and backlog prioritization are for.

## Related Documents

- `README.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/gated_turn-based_workflow-Codex.txt`
- `SolutionFolder/docs/CODING_STANDARDS.md`
- `SolutionFolder/docs/MVPVM_OVERVIEW.md`
