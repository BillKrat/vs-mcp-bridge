# VS MCP Bridge Technical Analysis

Last updated: 2026-04-04

## Purpose

This document is the living technical reference for the `vs-mcp-bridge` repository.

It serves two goals:

1. preserve architectural and operational context for current and future developers
2. surface missing features, constraints, and roadmap opportunities before the codebase grows into them accidentally

For a shorter external-review hand-off, also see [CHATGPT_HANDOFF.md](CHATGPT_HANDOFF.md).

## Executive Summary

`vs-mcp-bridge` is a local integration that exposes selected IDE or workspace state to AI tooling through MCP.

Current topology:

- `VsMcpBridge.McpServer` is a local MCP server running over stdio
- `VsMcpBridge.Vsix` is a Visual Studio extension running inside the IDE
- `VsMcpBridge.App` is a standalone WPF host that exercises the same shared bridge outside Visual Studio
- `VsMcpBridge.Shared` contains shared contracts, abstractions, diagnostics plumbing, pipe dispatch, and tool-window orchestration types
- `VsMcpBridge.Shared.Wpf` contains the reusable WPF tool-window views
- `VsMcpBridge.Shared.Tests` covers shared behavior
- `VsMcpBridge.Vsix.Tests` covers VSIX-specific composition and service logic

The design is intentionally conservative:

- the MCP side does not directly touch Visual Studio APIs
- each host owns its own runtime-specific interaction layer
- text edits are proposed as diffs and only applied after explicit approval in the tool window
- communication is local-only over a named pipe

The project is in a good early-platform state:

- the VSIX builds, packages, installs, and loads in the Experimental hive
- core services are behind interfaces and created through DI
- diagnostics exist at the package, tool window, service, pipe, and first-request levels
- unhandled exception persistence exists through a swappable sink abstraction
- shared bridge infrastructure is no longer coupled to the VSIX project
- the approval workflow now covers proposal creation, pending approval, reject, approve, apply, and failure states
- the standalone app is now functionally aligned with the current VSIX feature set within its non-VSIX host model
- unit coverage exists for DI registration, pipe dispatch, presenter/viewmodel behavior, approval workflow behavior, diff generation, and exception persistence

The largest remaining product gap is a robust machine-applicable edit model that preserves exact file formatting and line endings when approved edits are applied.

## Solution Layout

### `VsMcpBridge.Shared`

Target: `netstandard2.0`

Purpose:

- defines the bridge protocol between MCP server and VSIX
- provides the host-agnostic abstraction surface used by shared logic
- contains shared logic that can be reused by non-VSIX hosts

Current contents include:

- request/response and pipe envelope models
- `IBridgeLogger`
- `IUnhandledExceptionSink`
- `IAsyncPackage`
- `IThreadHelper`
- `IVsService`
- `IPipeServer`
- `IApprovalWorkflowService`
- `IEditApplier`
- tool-window interfaces
- `PipeServer`
- `FileUnhandledExceptionSink`
- `InMemoryApprovalWorkflowService`
- `LogToolWindowPresenter`
- `LogToolWindowViewModel`
- DI extensions for shared presenter/viewmodel services

Important note:

- the shared project still contains behavior that conceptually serves the Visual Studio host, but it no longer requires direct VSIX project references

### `VsMcpBridge.Shared.Wpf`

Target: `.NET Framework 4.7.2`

Purpose:

- hosts reusable WPF views that can stay outside the VSIX assembly
- keeps the view passive while presenter/viewmodel logic remains in the shared layer

Primary files:

- `Views/LogToolWindowControl.xaml`
- `Views/LogToolWindowControl.xaml.cs`

### `VsMcpBridge.Shared.Tests`

Target: `net8.0`

Purpose:

- unit tests around the shared layer
- validates behavior that should not require a VSIX-specific test host

Current tested areas:

- `PipeServer` request dispatch
- file-backed unhandled exception persistence
- presenter/viewmodel registration and behavior

### `VsMcpBridge.McpServer`

Target: `net8.0`

Purpose:

- hosts an MCP server over stdio
- exposes tool methods to the AI client
- forwards each tool call to the active host through a named pipe
- converts typed responses into MCP-friendly strings

Primary files:

- `Program.cs`
- `Pipe/PipeClient.cs`
- `Tools/VsTools.cs`

### `VsMcpBridge.App`

Target: `net8.0-windows`

Purpose:

- standalone reference host for developers who want to use the bridge outside a VSIX
- validates that non-VSIX runtime behavior can live outside the VSIX project
- reuses the shared pipe server, presenter/viewmodel, approval workflow, and WPF view

Primary files:

- `App.xaml.cs`
- `Composition/BridgeServiceCollectionExtensions.cs`
- `Services/StandaloneVsService.cs`
- `Services/FileEditApplier.cs`
- `Windows/MainWindow.xaml.cs`

### `VsMcpBridge.Vsix`

Target: `.NET Framework 4.7.2`

Purpose:

- Visual Studio extension hosted inside the IDE
- starts the named pipe listener
- translates pipe commands into Visual Studio API calls
- owns diagnostics, exception capture, and the tool-window shell

Primary files:

- `VsMcpBridgePackage.cs`
- `Composition/BridgeServiceCollectionExtensions.cs`
- `Services/VsService.cs`
- `Services/VsixEditApplier.cs`
- `Services/ThreadHelperAdapter.cs`
- `Logging/*`
- `Commands/ShowLogToolWindowCommand.cs`
- `ToolWindows/LogToolWindow.cs`

### `VsMcpBridge.Vsix.Tests`

Target: `.NET Framework 4.7.2`

Purpose:

- unit tests for VSIX-specific composition and service behavior
- avoids requiring a running Visual Studio instance

Current tested areas:

- DI registration
- package/service abstraction wiring
- `VsService` proposal lifecycle behavior
- `VsixEditApplier` reconstruction behavior through service-level tests

## Runtime Architecture

```text
AI client
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON line messages over named pipe "VsMcpBridge"
host implementation
  -> DTE / Visual Studio SDK / tool window   (VSIX)
  -> workspace / file system / build / window (App)
```

### Startup Flow

1. Visual Studio loads `VsMcpBridgePackage`.
2. The package initializes `ShowLogToolWindowCommand`.
3. The package builds a DI container through `BridgeServiceCollectionExtensions` and `MvpVmServiceExtensions`.
4. The container resolves the logger, exception sink, thread helper, VS service, pipe server, and tool-window presenter/viewmodel services.
5. Global unhandled exception handlers are registered.
6. `PipeServer.Start()` launches the listener thread.
7. When the tool window is created, `LogToolWindow` creates the WPF control and wires it to the DI-resolved presenter and viewmodel.

### Request Flow

1. An MCP tool is invoked on the stdio server.
2. `VsTools` calls `PipeClient`.
3. `PipeClient` serializes a `PipeMessage` and writes one line to the named pipe.
4. `PipeServer` accepts the connection and reads one line.
5. `PipeServer` deserializes the envelope and dispatches by `PipeCommands`.
6. `VsService` executes the requested Visual Studio operation.
7. The typed response is serialized back across the pipe.
8. `VsTools` formats the typed response into a text result for the MCP caller.

## Dependency Injection and Abstractions

Registered services in the VSIX host:

- `IBridgeLogger -> ActivityLogBridgeLogger`
- `IUnhandledExceptionSink -> FileUnhandledExceptionSink`
- `IApprovalWorkflowService -> InMemoryApprovalWorkflowService`
- `IEditApplier -> VsixEditApplier`
- `IThreadHelper -> ThreadHelperAdapter`
- `IVsService -> VsService`
- `IPipeServer -> PipeServer`
- `ILogToolWindowPresenter -> LogToolWindowPresenter`
- `ILogToolWindowViewModel -> LogToolWindowViewModel`
- `IAsyncPackage` as the current package instance
- `AsyncPackage` when the package instance supports the concrete Visual Studio type

Registered services in the standalone app host:

- `IBridgeLogger -> ConsoleBridgeLogger`
- `IUnhandledExceptionSink -> FileUnhandledExceptionSink`
- `IApprovalWorkflowService -> InMemoryApprovalWorkflowService`
- `IEditApplier -> FileEditApplier`
- `IThreadHelper -> DispatcherThreadHelper`
- `IVsService -> StandaloneVsService`
- `IPipeServer -> PipeServer`
- `ILogToolWindowPresenter -> LogToolWindowPresenter`
- `ILogToolWindowViewModel -> LogToolWindowViewModel`
- `IProposalDraftState -> AppSessionState`

Why this matters:

- testability is materially better than the original tightly-coupled shape
- logger and exception persistence are replaceable strategies
- the shared layer is now reusable by other hosts
- Visual Studio-specific concerns are kept in the VSIX project instead of leaking through every shared type

Current note:

- the shared DI seams are now exercised by both the VSIX and the standalone app, which is intentional because one of the app's purposes is to demonstrate non-VSIX hosting without polluting the VSIX project

## Implemented Capabilities

### `vs_get_active_document`

Returns:

- active document path
- language
- full text content

Host note:

- in the VSIX host this comes from Visual Studio's active document
- in the standalone app this comes from the current proposal/session state or a workspace fallback

### `vs_get_selected_text`

Returns:

- current document path
- selected text in the active editor

Host note:

- in the standalone app this is backed by proposal/session state rather than a live editor selection

### `vs_list_solution_projects`

Returns:

- project name
- project full path
- target framework moniker

Current limitation:

- nested solution folders and recursively nested projects are not fully modeled

### `vs_get_error_list`

Returns:

- current Visual Studio error list entries
- severity, description, file, line, project

Current limitation:

- `Code` and `Column` are not fully populated
- enumeration uses dynamic access and is fragile

Host note:

- the VSIX host reads the Visual Studio Error List
- the standalone app derives diagnostics from `dotnet build` output

### `vs_propose_text_edit`

Returns:

- a unified-diff-like string
- a pending approval entry in the tool window
- approval-triggered application through the VSIX host when the user accepts the proposal

Current limitation:

- the generated diff is both the user-facing preview and the apply source, so the apply path still reconstructs the entire document rather than applying a structured edit

## Tool Window UX

The VSIX currently exposes:

- a `View > VS MCP Bridge` menu entry
- a `VS MCP Bridge` tool window

The tool window currently has:

- manual proposal-entry fields
- a log text area
- pending approval text
- approve/reject buttons

Implementation shape:

- `LogToolWindowControl` is a passive WPF view
- `LogToolWindowPresenter` coordinates UI state and approval actions
- `LogToolWindowViewModel` exposes bindable properties and commands via `CommunityToolkit.Mvvm`

Most important gap:

- runtime logs are still not broadly forwarded into the presenter, so the log pane is underused
- the apply path still relies on diff reconstruction instead of a structured edit model

The standalone app exposes the same shared WPF control in a normal window and exists primarily as:

- a reference implementation for non-VSIX hosts
- a guardrail against pushing non-VSIX runtime behavior into the VSIX project

## Diagnostics and Observability

Current logger abstraction:

- `IBridgeLogger`
- implementation: `ActivityLogBridgeLogger`

Current exception persistence abstraction:

- `IUnhandledExceptionSink`
- implementation: `FileUnhandledExceptionSink`

Current persistence target:

- `%LocalAppData%\VsMcpBridge\Logs\UnhandledExceptions`

Useful current diagnostics:

- package initialization milestones
- pipe server startup
- first request handling
- VS service operation execution
- pipe failures
- persisted unhandled exception details

Current gaps:

- no request correlation IDs used as first-class tracing data
- no protocol versioning
- no capabilities or health endpoint
- no structured metrics
- limited runtime event forwarding into the tool window presenter

## Testing Posture

Current automated coverage includes:

- DI registration correctness
- singleton expectations
- pipe request dispatch behavior
- first-request logging behavior
- unknown command handling
- diff generation behavior
- proposal approval/rejection/application behavior
- file-backed exception persistence
- presenter/viewmodel behavior and command wiring

Current missing layers:

- live Visual Studio integration tests
- live named-pipe round trips between the two runtime processes
- package initialization in an Experimental instance
- standalone app integration coverage against a real workspace
- MCP server formatting tests
- end-to-end Visual Studio apply verification against real documents

## Build and Tooling Notes

### VSIX Build

The VSIX project must be built with Visual Studio MSBuild, for example:

```powershell
.\scripts\build-vsix.ps1 -Restore
```

`dotnet test` or SDK-hosted MSBuild is not the correct top-level build entry point for the whole solution because the old-style VSIX project depends on Visual Studio VSSDK build tasks.

### Test Execution

Current split:

- `VsMcpBridge.Shared.Tests` can be run with `dotnet test`
- `VsMcpBridge.App` can be built with `dotnet build`
- `VsMcpBridge.Vsix.Tests` should be built with Visual Studio `MSBuild.exe` and executed with `vstest.console.exe`

## Design Strengths

1. good process separation between MCP host and IDE host
2. clear shared abstraction layer after the recent decoupling
3. incremental DI adoption without unnecessary framework complexity
4. safety-first edit posture
5. explicit approval workflow with distinct proposal states
6. early exception-sink abstraction
7. better tool-window separation of concerns through presenter/viewmodel split
8. view extraction into `VsMcpBridge.Shared.Wpf` keeps the VSIX assembly thinner

## Architectural Weak Spots

1. the apply path rebuilds full document text from a display-oriented diff and can normalize line endings or other non-targeted formatting
2. no explicit protocol versioning or compatibility strategy
3. request IDs exist conceptually but are not fully exploited
4. tool coverage is still intentionally narrow
5. `VsService` is still broad and will likely need decomposition
6. error-list access is dynamic and brittle
7. no configuration model yet
8. no capabilities discovery surface
9. no durable persistence or audit model for user approvals
10. runtime tool-window logging is still only partially wired
11. host parity now exists, but it still relies on one broad service contract that may want decomposition into host-neutral sub-services later

## Suggested Near-Term Roadmap

### Phase 1

1. Route runtime bridge events into the tool window presenter.
2. Separate machine-applicable edits from the display diff used in the approval UI.
3. Add proposal IDs and request correlation IDs as first-class tracing data.
4. Add a bridge health/capabilities tool.
5. Add configuration for logging and exception sink selection.

### Phase 2

1. Add protocol versioning.
2. Add structured diagnostic events.
3. Add integration tests for real MCP-to-VSIX round trips.
4. Improve pipe failure handling and reporting.

### Phase 3

1. Expand solution/project modeling.
2. Add symbol and navigation queries.
3. Add current-document diagnostics and richer context retrieval.
4. Add build/test inspection tools.

### Phase 4

1. Add SQLite-backed exception persistence.
2. Add SQLite-backed proposal and approval history.
3. Introduce richer auditability for approval/application decisions.

## Recommended Next Abstractions

- `IBridgeConfigurationProvider`
- `IRequestContextAccessor`
- `IApprovalWorkflowService`
- `IEditApplier`
- `IEditProposalStore`
- `ICapabilitiesService`
- `IBridgeHealthService`
- `IDocumentService`
- `ISolutionService`
- `IDiagnosticsService`
- `IDiffService`

## Maintenance Rule

Update this document when any of these happen:

- a new MCP tool is added
- a new storage backend is introduced
- transport or protocol changes
- approval workflow behavior changes
- logging or diagnostic sinks change
- build/install requirements change
- major refactors change service boundaries

Keep [CHATGPT_HANDOFF.md](CHATGPT_HANDOFF.md) in sync whenever this document changes materially.

## Bottom Line

The architecture is directionally good.

The project does not need a rewrite. It needs the next layer of platform completeness:

- harden approval/apply so it uses a structured edit model
- formalize protocol/version/configuration concerns
- route live bridge state into the presenter
- expand capabilities without letting `VsService` become a dumping ground
- improve observability and persistence deliberately
