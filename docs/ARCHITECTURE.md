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

Request input behavior today:

- built-in prompt commands remain presenter-routed (`what is the active file`, `list solution projects`, `show error list`)
- non-built-in prompt input is now routed through host-registered `IChatRequestService`
- both hosts support prompt-box `ping` through this seam (`ping -> pong` outside OpenAI mode)

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

## Bridge Tool Boundary

Shared bridge tools execute through a catalog/executor boundary in `VsMcpBridge.Shared.Tools`.
The initial catalog is compiled and in-memory only; the first compiled regex text-search tool proves the catalog/executor path without changing MCP transport, proposal flow, or host startup behavior.
MEF discovery, directory-loaded tools, BM25 search, and host-specific tool packs are future extensions of this boundary, not part of the initial runtime path.

The executor owns the tool execution boundary logging contract.
Every tool execution must preserve request/operation correlation, return structured success/failure results, and emit enough start/completion/failure evidence that tools do not become black boxes during triage.
The durable baseline for this boundary is `docs/tool-execution-trace-workflow.md` with observed artifacts under `artifacts/logs/tool-regex-search-trace-20260509.*` and `docs/diagrams/tool-regex-search-trace-20260509.mmd`.

## Tool Execution Security Seams

Shared bridge tool execution now has minimal security contracts in `VsMcpBridge.Shared.Security`.
These contracts are insertion points only; they do not implement authentication, OAuth, sandboxing, MEF loading, or production plugin authorization.

Trust boundaries are defined this way:

- Host boundary: VSIX and standalone App hosts own host-specific services and privileges.
- Transport boundary: MCP stdio and the local named pipe move requests but do not authorize arbitrary behavior.
- MCP runtime boundary: `VsMcpBridge.McpServer` exposes only registered MCP tools and should remain transport-isolated from shared tool internals.
- Tool/plugin boundary: bridge tools execute behind `IBridgeToolExecutor`, not directly from callers.
- Downstream boundary: external services, file systems, Visual Studio APIs, model providers, and future plugin dependencies are treated as separate trust zones.

The executor is the shared enforcement point for foundational tool security behavior:

- `IToolExecutionPolicy` evaluates a `ToolExecutionSecurityContext` before a tool runs.
- `ISecurityRedactor` masks obvious secret-like values before payload-oriented logs or audit metadata are emitted.
- `IAuditSink` receives a structured `BridgeAuditEnvelope` after allowed, denied, successful, failed, canceled, or unknown-tool outcomes.
- request id and operation id remain part of the policy, audit, logging, and result path.

Current safe defaults preserve existing runtime behavior:

- `AllowToolExecutionPolicy` allows all current compiled tools.
- `NoOpAuditSink` records nothing unless a host or test overrides it.
- `BridgeSecurityRedactor` performs basic masking for obvious keys such as `apiKey`, `token`, `password`, `secret`, and bearer authorization values.

The durable security-aware evidence for the current compiled tool path is `docs/diagrams/tool-security-trace-20260509.mmd` with correlated logs and metadata under `artifacts/logs/tool-security-trace-20260509.*`.
That trace uses `RegexTextSearchTool` only and proves policy evaluation, payload redaction, tool execution, audit envelope emission, and request/operation correlation preservation without introducing MEF, plugin loading, OAuth/authentication, or real secret storage.

Tool and plugin authors are not responsible for core redaction, policy evaluation, or audit emission.
They still own their tool-specific validation and structured result shape, but the bridge execution boundary must continue to provide the shared security seams.

Deferred security work remains explicit:

- OAuth/authentication implementation
- real secret broker
- encrypted secret storage
- signed plugin manifests
- sandboxing
- remote transport authorization
- capability attestation
- distributed provenance chain
- tamper-evident audit store

Hooks and contract models only:

- secret references
- capability token model
- audit envelope model
- policy decision model

## Logging and Diagnostics Flow

Current logging behavior is intentionally boundary-focused for triage:

- App, VSIX, and MCP host configuration now flow through a shared `IConfiguration` bootstrap path with environment variables, prefixed environment variables, `appsettings.json`, and `%LocalAppData%\VsMcpBridge\appsettings.user.json` loaded in that order
- `VsMcpBridge.App` supports configuration-driven logging providers and appends provider-fed entries to the shared MVP/VM UI log surface
- App chat requests (including OpenAI mode) log request start/completion/failure with correlation IDs and elapsed timing
- `VsMcpBridge.McpServer` logs MCP chat-tool boundaries and named-pipe client request boundaries with command, request id, success state, and elapsed timing
- VSIX prompt-box chat requests now have host-side request handling through `IChatRequestService` with OpenAI/fake-mode compatibility behavior
- `PipeServer` logs dispatch boundaries with command/request id and elapsed timing, plus explicit cancel/failure paths
- `VsMcpBridge.Vsix` service operations log start/completion/failure with operation-level correlation and elapsed timing for the current read surface

Logging intent:

- `Trace` is the verbose class/process flow level and should make decoupled host/process boundaries diagnosable rather than opaque
- `Information` is the default user-facing informational level
- callers should avoid redundant duplicate `Trace` plus `Information` entries for the same event and instead choose the level that best matches the audience
- `Warning` and `Error` are reserved for actionable failure or exceptional conditions
- when Trace is enabled, Trace-level output should also be surfaced through the shared MVP/VM UI log view so it remains visible during rapid AI-assisted triage
- `StdErr` is the preferred out-of-band diagnostic channel for transport-safe external capture because it does not corrupt MCP stdio JSON traffic
- the shared bridge log sink now includes a forwarding seam, initially backed by file output, so SQL-backed or other persistence targets can later be added without changing core logging callers

Automation and diagramming requirement:

- every application workflow that matters for development or triage should remain runnable end-to-end through repeatable automation or a documented manual-plus-automation workflow
- each observed run should be able to produce a Mermaid sequence diagram from captured logs/artifacts that mirrors the actual application workflow
- new code should participate in the established logging pattern when it crosses a meaningful boundary, performs host/process work, calls external services, or can fail in a way future AI sessions must diagnose
- logs should include correlation IDs, operation names, success/failure state, and elapsed timing where applicable so AI can quickly localize the first missing or failing boundary
- avoid creating black-box paths: if a workflow cannot be traced from entry point to result, add the minimum logging/automation evidence needed before expanding that workflow further

Current durable workflow examples:

- `docs/app-host-ping-trace-workflow.md`
- `docs/vsix-host-ping-trace-workflow.md`
- `docs/vsix-host-selected-text-trace-workflow.md`

Diagnostic sinks remain transport-safe:

- MCP server file diagnostics: `%LocalAppData%\VsMcpBridge\Logs\McpServer\pipe-client.log`
- VSIX pipe trace diagnostics: `%LocalAppData%\VsMcpBridge\Logs\Vsix\pipe-server-trace.log`

These diagnostics are additive and are designed to avoid polluting MCP stdio JSON traffic.

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
- `vs_propose_text_edits`

## MCP Security Boundary

The MCP-exposed surface is intentionally explicit and limited.

- the MCP server registers a single tool container, `VsTools`, through `WithTools<VsTools>()`
- only methods in `VsTools` marked with `McpServerTool` are exposed over MCP
- the named-pipe hop does not accept arbitrary host actions; `PipeServer` dispatches only the hard-coded command allowlist defined in `PipeCommands`
- unknown, empty, malformed, or blank-command pipe requests fail closed with a serialized error response rather than being dispatched
- proposal/edit requests remain approval-gated: MCP can create proposals, but apply still happens only after an explicit approve action in the host UI
- there is no MCP tool for arbitrary shell or process execution; host-specific process launches that exist elsewhere in the codebase are not part of the registered MCP tool surface

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

1. `vs_propose_text_edit` and `vs_propose_text_edits` submit proposed edits through the MCP server.
2. The MCP request model is additive:
   - legacy single-file callers may continue sending `filePath`, `originalText`, and `proposedText`
   - multi-file callers may send `fileEdits`, where each entry contains `filePath`, `originalText`, and `proposedText`
3. The request crosses the named-pipe boundary and reaches the VSIX host.
4. In the tool window, `ProposalFilePath` can be entered manually or selected through a host-specific picker seam.
5. Picker selection flows through `ProposalFilePath`, and proposal-entry behavior still has a single authoritative load path.
6. When `ProposalFilePath` is valid, that load path populates both panes for the current full-document proposal workflow.
7. The left/original pane remains read-only, while the right/proposed pane stays editable until the proposal is submitted.
8. `Submit Proposal` is enabled only after the file loads successfully and the proposed text differs from the original text.
9. After submission, the proposal is routed into the tool window for approval or rejection, and the right/proposed pane becomes read-only while approval is pending.
10. The user explicitly approves or rejects the proposal in the tool window, but the click itself does not reset the proposal UI.
11. Terminal proposal outcomes drive the reset: pending approval state is cleared, the completed proposal callbacks cannot be reused, and proposal-entry state is refreshed from `ProposalFilePath`.
12. New proposals may carry `RangeEdit` or `RangeEdits` in addition to `Diff`, while the unified diff remains the operator-facing preview format and there is not yet a dedicated multi-range preview mode.
13. When `RangeEdits` are present, the tool window also shows a simple reviewed change list with sequence number, original segment, and updated segment for each reviewed range, and that list appears in both pending review and last completed proposal review.
14. Multi-file proposals now also show an Included Files list in both pending review and last completed proposal review so the operator can see proposal membership explicitly without changing the underlying diff-first review model.
15. Included Files is additive review metadata only. Its role is to make proposal membership explicit; it does not introduce per-file diff rendering, tabs, or a preview-engine change.
16. The same approval/apply pipeline handles both single-file and multi-file proposals.
17. If approved, apply validates every file edit before mutating any file.
18. Within each file, apply prefers range-based replacement when range metadata is present and falls back to full-document diff reconstruction when range metadata is absent.
19. Single-file multi-range apply remains all-or-nothing across the full range set for that file.
20. Multi-file apply remains all-or-nothing across the full proposal set.
21. If any intended range or file target no longer matches the approved original content, or if any file/range match becomes ambiguous, the entire proposal fails explicitly instead of guessing or partially applying.
22. If a later file write fails after earlier files were written, rollback restores the already-mutated files to their original approved state.
23. If the target already matches the approved updated content at an intended location or in an intended file, apply may skip that unit while preserving overall proposal success semantics when the rest still applies cleanly.
24. Otherwise, the approved replacement set is applied inside Visual Studio through the VSIX host while preserving untouched surrounding document content, line endings, and final trailing newline state where applicable.
25. `ProposalOutcomeMessageBuilder` is the centralized source for terminal outcome text and is shared across hosts so the VSIX and standalone app present the same outcome categories.
26. Standardized outcome categories are:
   - success
   - skip
   - drift failure
   - ambiguity failure
   - generic failure
   - rejection
27. Standardized messages include scope using file count, and proposal-wide failures explicitly state that no changes were applied.
28. The tool window enters a review-focused layout during both pending review and last completed review.
29. In that layout, the Original Text and Proposed Text comparison surface remains visible in compact form rather than being collapsed away.
30. The lower review surface receives more space than in authoring mode, while the log surface is deprioritized when empty so it does not visually dominate inspection.
31. This is a layout-emphasis change only. It does not introduce a new review model, per-file preview mode, or preview-engine change.
32. The tool window review surface remains compact, but it is now scrollable and splitter-resizable enough for practical inspection of pending and completed proposal content.
33. Live manual validation should focus on multi-range success, drift failure after submit and before approve, adjacent or nearby range behavior, multi-file success, and drift-safe multi-file failure with no partial apply.
34. Terminal status messages remain visible in the tool window after success, skip, reject, or failure, and apply failures are also written to the bridge logs.
35. The result is returned back through the bridge to the MCP client.

This can be summarized as:

`Propose -> Approve -> Apply -> Result`

Current verified state for this workflow:

- proposal creation works
- approval and apply were validated successfully
- a follow-up `vs_get_active_document` call succeeded after apply

Current limitation:

- preview remains unified-diff-based and diff-first rather than a dedicated multi-file or multi-range review mode
- Included Files clarifies membership, but review still does not separate changes into per-file diff surfaces
- review layout now prioritizes inspection better, but it still shares space with logs when the log area is populated
- outcome messages are clear in the existing status surface, but they are not yet visually emphasized beyond the compact review/status model
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
- `vs_propose_text_edits` now works through proposal creation and the shared approval/apply pipeline
- post-apply connectivity was verified with a successful follow-up `vs_get_active_document` call

Observed runtime note:

- `JsonRpc Warning: No target methods are registered that match "NotificationReceived"` was observed after apply, but it did not block the bridge or subsequent tool calls

## Related Documents

- `README.md`: quick orientation and build/test entry point
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living technical reference and roadmap
- `docs/MVPVM_OVERVIEW.md`: repository-specific UI pattern guide
- `docs/DIAGRAM_NOTES.md`: supporting notes for architecture diagrams
