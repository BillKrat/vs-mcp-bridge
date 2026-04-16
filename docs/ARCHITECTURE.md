# VS MCP Bridge Architecture

## Purpose

This document is the single source of truth for current system behavior in `vs-mcp-bridge`.

It consolidates the already-documented runtime flow, request lifecycle, approval workflow, and component relationships without introducing new concepts.

## High-Level Component Map

The solution is split into host-specific runtimes plus shared infrastructure:

- `VsMcpBridge.McpServer`: local MCP server that speaks stdio to an AI client
- `VsMcpBridge.Vsix`: Visual Studio extension that runs inside the IDE
- `VsMcpBridge.App`: standalone WPF host that demonstrates non-VSIX reuse of the bridge
- `VsMcpBridge.Shared`: shared contracts, abstractions, diagnostics, pipe dispatch, and tool-window orchestration
- `VsMcpBridge.Shared.Wpf`: reusable WPF views for the tool window UI
- `VsMcpBridge.Shared.Tests`: unit tests for the shared layer
- `VsMcpBridge.Vsix.Tests`: unit tests for VSIX-specific composition and service logic

Host behavior today:

- `VsMcpBridge.Vsix` provides Visual Studio-backed behavior through DTE and Visual Studio services
- `VsMcpBridge.App` provides workspace/file-system-backed behavior while reusing the same shared presenter, viewmodel, pipe server, approval workflow, and WPF view

The VSIX tool window is split this way:

- the view lives in `VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml`
- tool window state is exposed through a viewmodel
- UI orchestration lives in a presenter using an MVP/VM-style split
- bindings and commands use `CommunityToolkit.Mvvm`

## Core System Flow

Runtime flow:

```text
AI client
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON over named pipe "VsMcpBridge"
host implementation
  -> Visual Studio SDK / DTE APIs    (VSIX host)
  -> workspace / file system / build (App host)
```

The bridge is intentionally conservative at this stage:

- Visual Studio API access stays inside the VSIX
- the MCP server only talks to the VSIX over a local named pipe
- edits still require explicit approval in the tool window before they are applied
- diagnostics and unhandled exception capture are built in
- shared bridge infrastructure is decoupled from the VSIX so other hosts can provide their own implementations

## MCP Bridge Request Lifecycle

For a normal MCP tool call, the request path is:

1. The AI client sends an MCP tool request over stdio to `VsMcpBridge.McpServer`.
2. `VsMcpBridge.McpServer` resolves the requested tool and forwards a typed request through its named-pipe client.
3. The pipe client connects to the local named pipe `VsMcpBridge`.
4. `PipeServer` accepts the connection and reads the incoming `PipeMessage`.
5. `PipeServer` dispatches the request to the active host service layer through `VsService`.
6. `VsService` performs the host-specific work and returns a response.
7. `PipeServer` serializes and writes the pipe response.
8. The MCP server receives the pipe response and returns the tool result to the AI client over stdio.

This lifecycle is the same underlying bridge pattern for the currently exposed tools:

- `vs_get_active_document`
- `vs_get_selected_text`
- `vs_list_solution_projects`
- `vs_get_error_list`
- `vs_propose_text_edit`

## PipeServer and VsService Interaction

The named-pipe boundary separates transport concerns from host-specific behavior.

`PipeServer` owns:

- listening on the named pipe `VsMcpBridge`
- reading and writing serialized pipe messages
- dispatching incoming commands
- transport-level diagnostics and exception capture

`VsService` owns:

- Visual Studio-backed operations in the VSIX host
- request handling for the current tool surface
- proposal creation and approval-related host behavior

At a high level:

1. `PipeServer` receives a `PipeMessage`.
2. `PipeServer` determines the command and dispatches it.
3. `VsService` handles the requested operation.
4. `VsService` returns a typed response.
5. `PipeServer` returns that response across the named pipe.

## Approval Workflow

The edit path is proposal-based rather than silently applied.

Current approval flow:

1. `vs_propose_text_edit` submits the proposed edit through the MCP server.
2. The request crosses the named-pipe boundary and reaches the VSIX host.
3. In the tool window, `ProposalFilePath` can be entered manually or selected through a host-specific picker seam.
4. Picker selection flows through `ProposalFilePath`, and proposal-entry behavior still has a single authoritative load path.
5. When `ProposalFilePath` is valid, that load path populates both panes for the current full-document proposal workflow.
6. The left/original pane remains read-only, while the right/proposed pane stays editable until the proposal is submitted.
7. `Submit Proposal` is enabled only after the file loads successfully and the proposed text differs from the original text.
8. After submission, the proposal is routed into the tool window for approval or rejection, and the right/proposed pane becomes read-only while approval is pending.
9. The user explicitly approves or rejects the proposal in the tool window, but the click itself does not reset the proposal UI.
10. Terminal proposal outcomes drive the reset: pending approval state is cleared, the completed proposal callbacks cannot be reused, and proposal-entry state is refreshed from `ProposalFilePath`.
11. New proposals may carry `RangeEdit` or `RangeEdits` in addition to `Diff`, while the unified diff remains the operator-facing preview format and there is not yet a multi-range-specific preview mode.
12. If approved, apply prefers the single-file range-based replacement path when range metadata is present and falls back to full-document diff reconstruction when range metadata is absent.
13. Single-file multi-range apply validates every intended range against the current document before mutating any content and remains all-or-nothing across the entire range set.
14. If the target already matches the approved updated content at every intended location, apply becomes a no-op.
15. If any intended range no longer matches the approved original content, or if multiple candidate locations make any range match ambiguous, apply fails explicitly instead of guessing or partially applying.
16. Otherwise, the approved single-file replacement set is applied inside Visual Studio through the VSIX host while preserving untouched surrounding document content, line endings, and final trailing newline state where applicable.
17. Live manual validation should focus on multi-range success, drift failure after submit and before approve, and adjacent or nearby multi-range behavior; ambiguity failure is primarily an automated safety proof.
18. Terminal status messages remain visible in the tool window after success, skip, reject, or failure, and apply failures are also written to the bridge logs.
19. The result is returned back through the bridge to the MCP client.

This can be summarized as:

`Propose -> Approve -> Apply -> Result`

Current verified state for this workflow:

- proposal creation works
- approval and apply were validated successfully
- a follow-up `vs_get_active_document` call succeeded after apply

Current limitation:

- range-based apply is limited to one file; multi-file editing is still out of scope
- preview remains unified-diff-based and does not yet have a multi-range-focused presentation
- if `ProposalFilePath` reload fails at terminal completion, the proposal panes clear while the terminal status message remains visible

Low-priority UI backlog:

- clean up the bottom control layout in the tool window without changing the workflow
- investigate intermittent `GridSplitter` responsiveness in the tool window

## Current Verified Runtime Slice

The repository is past initial MCP bring-up and through end-to-end runtime validation for the current tool surface.

What is verified:

- the solution builds
- the VSIX project builds
- the named-pipe listener starts during package load
- Cursor can connect to the project-local MCP server through `.cursor/mcp.json`
- the current read-only MCP tools work end to end
- `vs_propose_text_edit` works through proposal, approval, and apply
- post-apply connectivity was verified with a successful follow-up `vs_get_active_document` call

Observed runtime note:

- `JsonRpc Warning: No target methods are registered that match "NotificationReceived"` was observed after apply, but it did not block the bridge or subsequent tool calls

## Related Documents

- `README.md`: quick orientation and build/test entry point
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living technical reference and roadmap
- `docs/MVPVM_OVERVIEW.md`: repository-specific UI pattern guide
- `docs/DIAGRAM_NOTES.md`: supporting notes for architecture diagrams
