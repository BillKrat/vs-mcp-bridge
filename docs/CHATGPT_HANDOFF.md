# VS MCP Bridge - ChatGPT Handoff

Last updated: 2026-04-10

## What This Document Is

This is the single briefing document to hand to ChatGPT so everyone starts from the same context.

It is intentionally practical and phase-specific.

Current goal:

- get the VSIX host, named-pipe bridge, and MCP server working end to end with the smallest correct changes

## Team Model

Current collaboration model:

- Bill: product direction, priorities, and final decisions
- ChatGPT: architectural direction, sequencing advice, and risk review
- Codex: in-repo implementation, verification, and documentation updates
- GitHub Copilot: optional in-editor assistance, but not the architectural source of truth

Working rule:

- prefer the smallest change that gets the bridge working
- do not broaden scope unless required for correctness

Important operating note:

- only Bill and Codex currently have workspace access
- ChatGPT does not directly read or write repository files in this phase
- this document is the primary handoff mechanism used to keep architectural guidance aligned with the real repository state

Practical expectation:

- Bill should not need to manually translate architecture into code changes alone
- ChatGPT and Codex should help drive the process, reduce avoidable detours, and keep the next step clear
- Bill's role is to provide direction, context, and decisions, not to already know the implementation details in advance

## Project Intent

`vs-mcp-bridge` is a local bridge between an AI client and a host that can expose IDE or workspace state through MCP.

Architecture intent:

- `VsMcpBridge.McpServer` owns MCP tool registration, validation, and forwarding
- `VsMcpBridge.Vsix` owns Visual Studio interactions
- communication between them is local named-pipe JSON
- edits remain proposal-based rather than silently applied

## Current Repository Shape

```text
VsMcpBridge.McpServer/     MCP stdio host and named-pipe client
VsMcpBridge.Vsix/          Visual Studio extension host
VsMcpBridge.App/           standalone WPF host for shared bridge pieces
VsMcpBridge.Shared/        shared contracts, diagnostics, services, MVP/VM support
VsMcpBridge.Shared.Wpf/    shared WPF views
VsMcpBridge.Shared.Tests/  shared-layer tests
VsMcpBridge.Vsix.Tests/    VSIX-facing tests
docs/                      project guidance and handoff notes
scripts/                   build helpers
```

## Current Phase

We are in the MCP connection phase.

Primary objective:

1. get the VSIX loading reliably
2. get the named-pipe bridge running reliably
3. get the MCP server talking to the host end to end
4. validate one basic tool flow before expanding features

Not the priority right now:

- redesigning the architecture
- broadening feature scope
- polishing non-essential abstractions
- adding new frameworks or hosts

## Constraints To Preserve

These constraints are intentional:

- no shell execution from the MCP server
- no direct file writes from the MCP server
- no filesystem access outside the solution root
- no autonomous agent loop
- preserve the VSIX/MCP server separation
- prefer minimal fixes over refactors

## Verified Status

Verified recently by Codex:

- the solution builds successfully with `dotnet build VsMcpBridge.slnx -v minimal`
- the VSIX project builds successfully
- the VSIX project was fixed for `net472` compatibility
- WPF support is enabled in the VSIX project
- the VSIX has the required `System.Text.Json` support for the current scaffold
- the VSIX source includes a command table and command wiring for opening the tool window

Important environment limit:

- actual Visual Studio Experimental Instance runtime validation has not yet been completed from the current Codex workspace because that environment is not a Windows Visual Studio host

## What Still Needs Real Validation

These are the next real-world checks:

1. the VSIX package loads in the Experimental Instance
2. the tool window opens without throwing
3. the named-pipe listener starts when the package loads
4. the MCP server can connect to the pipe
5. one read-only tool call succeeds end to end

## Current Risk Areas

Top near-term risks:

1. build success is ahead of runtime validation
2. pipe startup and connection behavior may fail even though both sides compile
3. the current Error List implementation is compile-safe but still needs real Visual Studio runtime confirmation
4. existing line-ending churn in the worktree should not be mixed into functional changes unless cleaned intentionally

## Execution Order We Want

Please optimize recommendations for this order:

1. validate VSIX Experimental Instance startup
2. validate package load
3. validate tool-window open
4. validate named-pipe listener startup
5. validate MCP server to VSIX round-trip with one read-only tool
6. only then recommend broader hardening or feature expansion

## What We Want From ChatGPT

We want architectural help that is grounded in the current phase, not a wholesale redesign.

Most useful outputs right now:

1. the next 3-5 concrete engineering steps
2. the highest-risk boundary issues at the MCP-server to pipe to VSIX boundary
3. the minimum observability or diagnostics needed if connection debugging becomes difficult
4. any minimum architectural guardrails we should preserve while making the bridge functional

## Preferred Guidance Style

Please keep recommendations:

- concrete
- phase-appropriate
- minimal in scope
- aligned to the existing repository rather than an idealized rewrite

If you think the current code or docs are inconsistent, call that out explicitly instead of silently assuming the larger architecture is already complete.

Please also assume:

- the human collaborator is learning the system while building it
- recommendations should help maintain momentum rather than increase process overhead
- if a simpler path exists, prefer it and say so clearly
- if the current path should stay unchanged for now, say that directly instead of proposing extra infrastructure

## Key Files To Review

If you want to ground recommendations in the code, start here:

- `VsMcpBridge.McpServer/Program.cs`
- `VsMcpBridge.McpServer/Pipe/PipeClient.cs`
- `VsMcpBridge.McpServer/Tools/VsTools.cs`
- `VsMcpBridge.Vsix/VsMcpBridgePackage.cs`
- `VsMcpBridge.Vsix/Pipe/PipeServer.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindowControl.xaml`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindowControl.xaml.cs`

## Suggested Prompt To Use With ChatGPT

Use something close to this:

> Read this handoff carefully and treat it as the current project brief. We are working on `vs-mcp-bridge`, and the immediate objective is to get the VSIX host, named-pipe transport, and MCP server working end to end with the smallest correct changes. Do not propose a broad redesign unless it is required for correctness. Based on the current architecture and execution order in this handoff, identify the next 3-5 concrete engineering steps, the most important runtime and boundary risks, and any minimum architectural guardrails we should preserve while making the bridge functional.
