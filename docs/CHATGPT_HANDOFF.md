# VS MCP Bridge ChatGPT Hand-Off

Last updated: 2026-04-04

## Purpose

This document is optimized for handing the repository to ChatGPT or another external reviewer so it can understand the current state quickly and recommend next steps without spending most of its time rebuilding context.

It should be kept aligned with `README.md`, `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`, and `docs/MVPVM_OVERVIEW.md`.

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

- `VsMcpBridge.Shared` contains host-agnostic interfaces such as `IAsyncPackage`, `IThreadHelper`, `IVsService`, `IBridgeLogger`, `IUnhandledExceptionSink`, `IPipeServer`, and presenter-facing coordination interfaces used by multiple hosts.
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
- `docs/MVPVM_OVERVIEW.md`
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
- `VsMcpBridge.Shared/Interfaces/*`
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
- `VsMcpBridge.Shared.Tests/*`
- `VsMcpBridge.Vsix.Tests/*`

## Suggested Prompt

Use something close to this:

> Review this repository as an architect and implementation planner. The shared layer has been decoupled from the VSIX, the WPF tool window view now lives in `VsMcpBridge.Shared.Wpf`, the tool window follows an MVP/VM split documented in `docs/MVPVM_OVERVIEW.md`, and there is now both a VSIX host and a standalone app host that reuse the shared bridge. Based on the current architecture, identify the top risks, the most important missing abstractions, and the next 3-5 engineering steps that should be taken to harden the approval/apply workflow, improve observability, and keep the host-service layer from growing into a catch-all service. Prefer concrete recommendations over generic advice.
