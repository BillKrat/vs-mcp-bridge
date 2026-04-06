# VS MCP Bridge — AI Handoff

Last updated: 2026-04-06

## Purpose

This document is the shared communication channel between the human developer (Bill), ChatGPT (architect), GitHub Copilot (pair-programmer), and Codex (implementer). It is optimized for handing context quickly without rebuilding it from scratch.

It should be kept aligned with `README.md`, `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`, `docs/MVPVM_OVERVIEW.md`, and `docs/CODING_STANDARDS.md`.

---

## Living Document Protocol

This file has multiple AI contributors. To avoid stomping on each other:

1. **Bill (human)** owns the top-level architecture sections (Summary, Project Layout, Architecture, Supported Operations, Current State, Next Steps).
2. **Each AI** owns exactly one section at the bottom, named `## [AI Name] — Notes`. Only that AI appends to its own section.
3. **Append-only within AI sections** — new entries go at the top, oldest at the bottom. Never edit a prior entry.
4. **Disagreements** go in your own section as a note, not as an edit to another section.
5. **Propose changes** to Bill's sections by adding a `### Proposed Update` block in your AI section — Bill decides whether to apply it.
6. **`docs/CODING_STANDARDS.md`** is the canonical source for patterns and conventions. Propose additions there via a `### Proposed Standards Addition` block in your AI section.

---

## One-Paragraph Summary

`vs-mcp-bridge` is a local bridge between an AI client and a host process that can surface IDE or workspace state. A `net8.0` MCP server (`VsMcpBridge.McpServer`) speaks stdio to the AI client and forwards JSON commands over a named pipe to a host implementation. Today that host can be either the Visual Studio extension (`VsMcpBridge.Vsix`, .NET Framework 4.7.2) or the standalone WPF sample app (`VsMcpBridge.App`, net8.0-windows). The shared layer (`VsMcpBridge.Shared`) contains protocol models plus shared abstractions, diagnostics plumbing, pipe dispatch, approval workflow, and presenter/viewmodel logic, while `VsMcpBridge.Shared.Wpf` contains the reusable tool-window views. The system currently supports read/query operations plus diff proposal, approval, rejection, and apply in both hosts, with host-specific implementations behind shared interfaces.

The current architectural theme is: keep host-specific integration in the host project, keep shared coordination and contracts outside it, use the standalone app as the reference example for non-VSIX adoption, and keep the WPF tool window split along MVP/VM lines.

## Current Project Layout

```text
VsMcpBridge.Shared/        shared contracts, abstractions, pipe server, diagnostics, MVP/VM
VsMcpBridge.Shared.Wpf/    shared WPF tool-window views
VsMcpBridge.Shared.Tests/  tests for shared behavior
VsMcpBridge.McpServer/     MCP stdio host and named-pipe client
VsMcpBridge.App/           standalone WPF host and non-VSIX runtime implementations
VsMcpBridge.Vsix/          Visual Studio extension, VS service implementation, package bootstrap
VsMcpBridge.Vsix.Tests/    tests for VSIX composition and VSIX-specific service logic
```

## Current Architecture

Runtime flow:

```text
AI client
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON over named pipe "VsMcpBridge"
host implementation
  -> Visual Studio SDK / DTE APIs    (VSIX)
  -> workspace / file system / build (App)
```

Important boundary:

- `VsMcpBridge.Shared` contains host-agnostic interfaces such as `IAsyncPackage`, `IThreadHelper`, `IVsService`, `ILogLevelSettings`, `IUnhandledExceptionSink`, `IPipeServer`, and presenter-facing coordination interfaces used by multiple hosts. Logging uses `Microsoft.Extensions.Logging.ILogger` — the custom `IBridgeLogger` has been removed.
- `VsMcpBridge.Vsix` supplies Visual Studio-specific implementations.
- `VsMcpBridge.App` supplies standalone workspace/file-system implementations and is the example host for non-VSIX consumers.
- This split exists so other hosts can reuse bridge infrastructure by supplying their own implementations instead of referencing the VSIX directly.

## Current Supported Operations

Implemented MCP-facing operations:

- get active document
- get selected text
- list solution projects
- get error list
- propose text edit as a diff
- approve or reject a pending proposal in the tool window
- apply an approved proposal through the VSIX host

Current operating model:

- Visual Studio API access stays inside the VSIX
- non-VSIX behavior lives in the standalone app instead of leaking into the VSIX
- transport is a local named pipe
- edits require explicit tool-window approval before they are applied
- diagnostics and unhandled exception persistence are built in

## Important Current State

What is solid:

- clean process split
- shared abstraction layer now exists
- DI seams are in place
- there is now a working standalone host that exercises the shared layer outside Visual Studio
- unhandled exception persistence is abstracted
- tool window view/presenter/viewmodel split exists
- the WPF view now lives in `VsMcpBridge.Shared.Wpf` instead of the VSIX assembly
- the presenter now owns shared UI coordination while host-specific lifecycle remains in the host app/package entry point
- there is a repo-specific MVP/VM guide for future UI work
- both shared and VSIX layers have automated tests

What is still incomplete:

- the apply path rebuilds full document text from the generated diff instead of using a structured edit model
- runtime bridge events are not fully routed into the presenter/tool window
- protocol/version/capabilities handling is still minimal
- `VsService` is still a broad service that will likely need decomposition

## Recent Changes That Matter

Recent refactoring moved key infrastructure out of the VSIX project into `VsMcpBridge.Shared`.

That includes:

- pipe server
- unhandled exception sink
- presenter/viewmodel logic
- shared service interfaces
- thread/package abstractions needed to keep the shared layer host-agnostic

Follow-up work completed after that refactor:

- added `IApprovalWorkflowService`, `EditProposal`, and proposal status tracking
- completed tool-window proposal submission plus approve/reject/apply flow
- added `VsixEditApplier` for Visual Studio-hosted application of approved proposals
- added `VsMcpBridge.App` as a standalone WPF host for the shared bridge
- replaced the app's initial null services with working file-system/workspace-backed implementations so it is functionally aligned with the current VSIX feature set within its host model
- added a shared proposal-draft state seam so presenter behavior can be reused without coupling shared code to the app assembly
- moved the WPF control into the new `VsMcpBridge.Shared.Wpf` project
- added `scripts/build-vsix.ps1` to standardize VSIX builds across Insiders and Community MSBuild installs
- added `docs/MVPVM_OVERVIEW.md` to document how presenter/viewmodel/view responsibilities are expected to stay split in this repo

## Current Test Status

Passing now:

- `dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj`
- `dotnet build .\VsMcpBridge.App\VsMcpBridge.App.csproj`
- `MSBuild.exe .\VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj /restore /t:Build /p:Configuration=Debug /p:Platform=AnyCPU`
- `vstest.console.exe .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll`

Important caveat:

- `dotnet test .\VsMcpBridge.slnx` is not the correct full-solution runner because the old-style VSIX build depends on Visual Studio MSBuild/VSSDK tasks rather than the SDK-hosted `dotnet` MSBuild path.

Practical note:

- recent manual testing has been reported as good, but this file should not imply there is full end-to-end automated coverage for either host's real edit application path

## Risks / Weak Spots ChatGPT Should Evaluate

Focus on these:

1. approved edits are still applied by reconstructing full file text from a diff preview
2. request identity and protocol versioning are still weak
3. host service implementations are at risk of becoming overly broad if the current `IVsService` shape is not decomposed
4. error-list access uses dynamic typing and is brittle
5. current diff generation is display-friendly but not yet a robust machine-applicable edit model
6. there is still no real capabilities or bridge-health surface

## Questions To Ask ChatGPT

Use questions like these:

1. Given the current split, what is the best design for a safe approval/apply workflow?
2. How should proposal/application be modeled as structured edits instead of plain diffs?
3. How should the current host service shape be decomposed next without adding unnecessary complexity?
4. What protocol/versioning changes should be introduced before tool count grows?
5. What minimum observability model should be added next?
6. Which additional host capabilities provide the best value per unit of implementation complexity, and which should remain VSIX-only?

## Most Likely Best Next Steps

1. Separate machine-applicable edit models from the human-readable diff output used in the approval UI.
2. Route more runtime proposal/log events into the tool window presenter.
3. Add durable storage for proposal history and approval/application outcomes.
4. Add bridge capabilities/health reporting and protocol versioning.
5. Start decomposing the current host service shape into document, solution, diagnostics, and edit-related services.

## Files ChatGPT Should Read First

- `README.md`
- `docs/CODING_STANDARDS.md` ← **read this before generating any code**
- `docs/MVPVM_OVERVIEW.md`
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
- `VsMcpBridge.Shared/Interfaces/*`
- `VsMcpBridge.Shared/Loggers/LoggerBase.cs`
- `VsMcpBridge.Shared/Services/PipeServer.cs`
- `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- `VsMcpBridge.Shared/MvpVm/LogToolWindowViewModel.cs`
- `VsMcpBridge.Shared.Wpf/Views/LogToolWindowControl.xaml`
- `VsMcpBridge.App/App.xaml.cs`
- `VsMcpBridge.App/Composition/BridgeServiceCollectionExtensions.cs`
- `VsMcpBridge.App/Services/StandaloneVsService.cs`
- `VsMcpBridge.Vsix/VsMcpBridgePackage.cs`
- `VsMcpBridge.Vsix/Composition/BridgeServiceCollectionExtensions.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Vsix/Services/VsixEditApplier.cs`
- `VsMcpBridge.Vsix/Logging/ActivityLogBridgeLogger.cs`
- `VsMcpBridge.Shared.Tests/*`
- `VsMcpBridge.Vsix.Tests/*`

## Suggested Prompt

Use something close to this:

> Review this repository as an architect and implementation planner. The shared layer has been decoupled from the VSIX, the WPF tool window view now lives in `VsMcpBridge.Shared.Wpf`, the tool window follows an MVP/VM split documented in `docs/MVPVM_OVERVIEW.md`, and there is now both a VSIX host and a standalone app host that reuse the shared bridge. Logging now uses `Microsoft.Extensions.Logging.ILogger` throughout with a `LoggerBase` template-method hierarchy and a runtime-switchable `ILogLevelSettings` singleton. Read `docs/CODING_STANDARDS.md` before proposing any code changes. Based on the current architecture, identify the top risks, the most important missing abstractions, and the next 3-5 engineering steps that should be taken to harden the approval/apply workflow, improve observability, and keep the host-service layer from growing into a catch-all service. Prefer concrete recommendations over generic advice.

---

## GitHub Copilot — Notes

*I am GitHub Copilot. I work with Bill directly as a pair-programmer. New entries go at the top of this section.*

---

### Entry: 2026-04-06 — Logging Architecture, Async Safety, and Recent Branch Work

**Branch**: `feature/approval-apply-ui-slice` (4 commits ahead of `main` / `7ee970f`)

**What changed since the branch point** (`68f0552`, `516d5b3`, `0cba45b`, `372fad2`):

| Area | Change |
|---|---|
| `IBridgeLogger` | **Removed.** All logging now uses `Microsoft.Extensions.Logging.ILogger`. |
| `LoggerBase` | New template-method base class. Subclasses override `LogMessage`; base owns `IsEnabled` and dispatch. |
| `DebugBridgeLogger` | New. Writes to `System.Diagnostics.Debug.WriteLine` → VS Output window. |
| `ActivityLogBridgeLogger` | Refactored to extend `LoggerBase`. Chains `DebugBridgeLogger` as `AdditionalLogger`. |
| `ConsoleBridgeLogger` | Moved from `VsMcpBridge.App/Logging/` to `VsMcpBridge.Shared/Loggers/`. Extends `LoggerBase`. |
| `ILogLevelSettings` / `LogLevelSettings` | New. Runtime log-level switch shared by loggers and the tool-window dropdown. |
| `LogToolWindowViewModel` | Added `SelectedLogLevel` + `AvailableLogLevels` bound to `ILogLevelSettings`. |
| `ServiceProviderExtensions.Resolve<T>` | Now emits `LogTrace("[DI] Resolving {TypeName}")`. Composition roots converted to use it. |
| Test doubles | Consolidated and moved to `VsMcpBridge.Shared/Tests/` for reuse across test projects. |
| `TaskScheduler.UnobservedTaskException` | **Removed from VSIX.** It was catching all of VS's own internal task timeouts. `AppDomain.CurrentDomain.UnhandledException` is sufficient in the VSIX. It remains appropriate in `VsMcpBridge.App` (a controlled process). |
| `CODING_STANDARDS.md` | **New file** at `docs/CODING_STANDARDS.md`. Canonical patterns reference. |

**Key patterns to preserve going forward:**

1. Every fire-and-forget (`_ = SomeAsync()`) must have an internal `try-catch`. There is no VSIX-level safety net. See `CODING_STANDARDS.md §1`.
2. `LogError` is `(Exception, string, ...)` — exception first. This is the opposite of the old `IBridgeLogger` order.
3. Composition roots use `Resolve<T>()` — never `GetRequiredService<T>()` at those sites.
4. `ConsoleBridgeLogger` and `ActivityLogBridgeLogger` both require `ILogLevelSettings` injected by DI to participate in the runtime level switch. Their parameterless constructors exist only for bootstrap / test use.

**What I'd flag for ChatGPT's next architecture review:**
- `LogToolWindowPresenter.SubmitProposalAsync` calls `_serviceProvider.GetRequiredService<IVsService>()` directly — this should use `Resolve<T>()` for consistency and traceability.
- The `LogLevel.None` entry in `AvailableLogLevels` effectively silences the logger. Intentional, but worth documenting in the UI as "Off" rather than "None" for end-user clarity.
- `RecordingBridgeLogger` in `VsMcpBridge.Shared/Loggers/` now extends `LoggerBase` and stores `Errors` as `(string Message, Exception? Exception)` — tests asserting `logger.Errors` need to use the tuple pattern.

---

## Codex - Notes

*I am Codex. I work in-repo as the implementation and documentation partner. New entries go at the top of this section.*

---

### Entry: 2026-04-05 - Commit Review, Collaboration Routing, and ChatGPT Ramp-Up

Reviewed commits from April 5, 2026:

- `68f055282cc9048b3544e8257a915f1f0e7599b8` - removed redundant classes and moved reusable pieces into `VsMcpBridge.Shared`
- `516d5b3dad02f78ef20d47744c117399b9c5beeb` - removed `IBridgeLogger` and moved to standard MEL logging
- `0cba45b99a720fd9d378718a36e6737a53a0090e` - plumbed loggers, runtime log level settings, and tool-window log-level binding
- `372fad2c601fad1ef6bcdbaefe2f4ce7d566e3a7` - VSIX logger/package cleanup
- `8b29e8a0da247210a597248661fe8a15816e20ec` - added `docs/CODING_STANDARDS.md` and expanded this handoff file

What matters from those commits:

- `IPipeClient` is no longer MCP-server-local; it now lives in `VsMcpBridge.Shared/Interfaces/IPipeClient.cs`, which is the right direction for host-agnostic contracts.
- Logging now consistently uses `Microsoft.Extensions.Logging.ILogger` with shared logger infrastructure in `VsMcpBridge.Shared/Loggers/`.
- `ILogLevelSettings` and `LogLevelSettings` make logging behavior runtime-switchable from the tool window rather than hard-coded.
- Test support classes were consolidated under `VsMcpBridge.Shared/Tests/`, which reduces duplicated host-specific test scaffolding.
- `docs/CODING_STANDARDS.md` is now the right place for implementation guardrails. `docs/AI_HANDOFF.md` should stay focused on state, risks, and cross-AI coordination.

Documentation-relevant corrections and cautions:

- Copilot's current note says `TaskScheduler.UnobservedTaskException` remains appropriate in `VsMcpBridge.App`, but the current code in `VsMcpBridge.App/App.xaml.cs` no longer subscribes to it there either. Treat the code as authoritative.
- `docs/CODING_STANDARDS.md` says composition roots should use `Resolve<T>()`, but `VsMcpBridge.App/App.xaml.cs` still uses `GetRequiredService<T>()` for `_exceptionSink` and `_pipeServer`. Either the app startup should be updated to match the standard, or the standard should be narrowed to say it currently applies to intended composition-root usage rather than all existing composition roots.

Collaboration routing I recommend:

- Put durable working agreements in `docs/AI_COLLABORATION.md`.
- Keep `docs/AI_HANDOFF.md` as the living state document plus append-only AI notes.
- Keep `docs/CODING_STANDARDS.md` narrowly focused on code-generation and implementation rules.

ChatGPT ramp-up guidance:

- ChatGPT should read `docs/CODING_STANDARDS.md`, then `docs/AI_HANDOFF.md`, then `docs/AI_COLLABORATION.md`, then the architecture/code files listed in this handoff.
- ChatGPT should respond with architecture and planning guidance, not broad repository restatement.
- If ChatGPT wants top-level handoff changes, it should propose them explicitly instead of rewriting AI-owned sections.

### Proposed Update

Add `docs/AI_COLLABORATION.md` to the "Files ChatGPT Should Read First" list immediately after `docs/AI_HANDOFF.md` and before deeper architecture docs.
