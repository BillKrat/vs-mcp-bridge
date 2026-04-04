# VS MCP Bridge ChatGPT Hand-Off

Last updated: 2026-04-04

## Purpose

This document is optimized for handing the repository to ChatGPT or another external reviewer so it can understand the current state quickly and recommend next steps without spending most of its time rebuilding context.

## One-Paragraph Summary

`vs-mcp-bridge` is a local bridge between an AI client and Visual Studio. A `net8.0` MCP server (`VsMcpBridge.McpServer`) speaks stdio to the AI client and forwards JSON commands over a named pipe to a Visual Studio extension (`VsMcpBridge.Vsix`, .NET Framework 4.7.2). The VSIX owns all Visual Studio SDK/DTE interaction. The shared layer (`VsMcpBridge.Shared`) now contains protocol models plus shared abstractions, diagnostics plumbing, pipe dispatch, and the presenter/viewmodel logic for the tool window. The system currently supports read/query operations plus diff proposal, but not approval-and-apply.

## Current Project Layout

```text
VsMcpBridge.Shared/        shared contracts, abstractions, pipe server, diagnostics, MVP/VM
VsMcpBridge.Shared.Tests/  tests for shared behavior
VsMcpBridge.McpServer/     MCP stdio host and named-pipe client
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
VsMcpBridge.Vsix
  -> Visual Studio SDK / DTE APIs
Visual Studio IDE
```

Important boundary:

- `VsMcpBridge.Shared` contains host-agnostic interfaces such as `IAsyncPackage`, `IThreadHelper`, `IVsService`, `IBridgeLogger`, `IUnhandledExceptionSink`, and `IPipeServer`.
- `VsMcpBridge.Vsix` supplies the Visual Studio-specific implementations.
- This split was introduced so other hosts can reuse bridge infrastructure by supplying their own implementations instead of referencing the VSIX directly.

## Current Supported Operations

Implemented MCP-facing operations:

- get active document
- get selected text
- list solution projects
- get error list
- propose text edit as a diff

Current operating model:

- Visual Studio API access stays inside the VSIX
- transport is a local named pipe
- no direct write/apply path exists yet
- diagnostics and unhandled exception persistence are built in

## Important Current State

What is solid:

- clean process split
- shared abstraction layer now exists
- DI seams are in place
- unhandled exception persistence is abstracted
- tool window view/presenter/viewmodel split exists
- both shared and VSIX layers have automated tests

What is still incomplete:

- `vs_propose_text_edit` stops at diff generation
- no approval workflow coordinator exists
- no apply-edits implementation exists
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

- fixed `VsService` so its injected `IThreadHelper` is stored and used
- fixed `VsMcpBridgePackage` so `IAsyncPackage.GetServiceAsync<T>` forwards to `AsyncPackage` instead of throwing
- added `ThreadHelperAdapter` and registered it in DI
- created `VsMcpBridge.Shared.Tests`
- moved shared-only tests out of `VsMcpBridge.Vsix.Tests`
- updated the VSIX test project to reference the VSIX project directly instead of a stale built DLL

## Current Test Status

Passing now:

- `dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj`
- `MSBuild.exe .\VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj /restore /t:Build /p:Configuration=Debug /p:Platform=AnyCPU`
- `vstest.console.exe .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll`

Important caveat:

- `dotnet test .\VsMcpBridge.slnx` is not the correct full-solution runner because the old-style VSIX build depends on Visual Studio MSBuild/VSSDK tasks rather than the SDK-hosted `dotnet` MSBuild path.

## Risks / Weak Spots ChatGPT Should Evaluate

Focus on these:

1. approval/apply workflow is still missing
2. request identity and protocol versioning are still weak
3. `VsService` is at risk of becoming an overly broad service
4. error-list access uses dynamic typing and is brittle
5. current diff generation is display-friendly but not a robust machine-applicable edit model
6. there is still no real capabilities or bridge-health surface

## Questions To Ask ChatGPT

Use questions like these:

1. Given the current split, what is the best design for a safe approval/apply workflow?
2. Should proposal/application be modeled as structured edits instead of plain diffs?
3. How should `VsService` be decomposed next without adding unnecessary complexity?
4. What protocol/versioning changes should be introduced before tool count grows?
5. What minimum observability model should be added next?
6. Which additional Visual Studio capabilities provide the best value per unit of implementation complexity?

## Most Likely Best Next Steps

1. Introduce an approval workflow service with proposal IDs and status.
2. Route proposal/log events into the tool window presenter.
3. Separate machine-applicable edit models from human-readable diff output.
4. Add bridge capabilities/health reporting and protocol versioning.
5. Start decomposing `VsService` into document, solution, diagnostics, and edit-related services.

## Files ChatGPT Should Read First

- `README.md`
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
- `VsMcpBridge.Shared/Interfaces/*`
- `VsMcpBridge.Shared/Services/PipeServer.cs`
- `VsMcpBridge.Shared/MvpVm/LogToolWindowPresenter.cs`
- `VsMcpBridge.Vsix/VsMcpBridgePackage.cs`
- `VsMcpBridge.Vsix/Composition/BridgeServiceCollectionExtensions.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Shared.Tests/*`
- `VsMcpBridge.Vsix.Tests/*`

## Suggested Prompt

Use something close to this:

> Review this repository as an architect and implementation planner. The shared layer was recently decoupled from the VSIX so other hosts can provide their own implementations. Based on the current architecture, identify the top risks, the most important missing abstractions, and the next 3-5 engineering steps that should be taken to complete the approval/apply workflow and harden the bridge for growth. Prefer concrete recommendations over generic advice.
