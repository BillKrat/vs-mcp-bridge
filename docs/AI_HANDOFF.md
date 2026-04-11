# VS MCP Bridge - AI Handoff

Last updated: 2026-04-10

## Purpose

This document is the fast handoff for Bill, ChatGPT, Codex, and GitHub Copilot.

Its job is to answer three questions quickly:

1. what the repository actually looks like now
2. what has been verified so far
3. what we should do next

Read this after `docs/CODING_STANDARDS.md`.

## Current Phase

The repository is in the MCP connection phase.

Primary objective:

- get the VSIX host, named-pipe transport, and MCP server working together end to end

Secondary objective:

- keep the architecture clean enough that we do not have to undo rushed decisions later

Out of scope for this phase unless required for correctness:

- broad feature expansion
- redesigning the approval model
- adding new hosts or frameworks
- polishing documentation beyond what is needed to keep execution aligned

## Repository Snapshot

Current solution layout:

```text
VsMcpBridge.McpServer/     MCP stdio host and named-pipe client
VsMcpBridge.Vsix/          Visual Studio extension host
VsMcpBridge.App/           standalone WPF host for shared bridge pieces
VsMcpBridge.Shared/        shared contracts, services, MVP/VM support, diagnostics
VsMcpBridge.Shared.Wpf/    shared WPF views
VsMcpBridge.Shared.Tests/  shared-layer tests
VsMcpBridge.Vsix.Tests/    VSIX-facing tests
docs/                      collaboration, handoff, technical notes
scripts/                   build helpers
```

Architecture intent:

- `VsMcpBridge.McpServer` owns MCP tool registration, validation, and forwarding
- `VsMcpBridge.Vsix` owns Visual Studio interaction
- communication between them is local named-pipe JSON
- edits remain proposal-based and require approval before apply

## Verified Current Status

Verified recently:

- the solution builds successfully with `dotnet build VsMcpBridge.slnx -v minimal`
- the VSIX project builds successfully
- the VSIX project has the minimum references needed for WPF plus `System.Text.Json` on `net472`
- the VSIX source includes a command table and tool-window command wiring for opening the log window

Important environment limit:

- runtime validation of actual Visual Studio Experimental Instance loading and interactive tool-window opening has not been completed from this workspace because the current execution environment is not a Windows Visual Studio host

## Current Technical Reality

What appears solid enough to keep building on:

- the solution structure is clear
- the MCP server and VSIX remain separated
- named-pipe transport exists on both sides
- the shared layer is present and the repo is not just a VSIX-only spike
- the code now compiles after the recent VSIX compatibility fixes

What still needs practical validation:

- whether the VSIX loads cleanly in the Experimental Instance
- whether the named-pipe server actually starts when the package loads
- whether the MCP server can connect to the pipe reliably
- whether a basic tool call succeeds end to end
- whether the tool window can be opened in real Visual Studio without runtime exceptions

## Immediate Execution Order

The next work should stay in this order:

1. validate VSIX Experimental Instance startup
2. validate package load
3. validate tool-window open
4. validate named-pipe listener startup
5. validate MCP server to VSIX round-trip with one basic read-only tool
6. only then expand capabilities or improve workflow depth

## Known Risks

Current top risks:

1. VSIX runtime behavior is less verified than build status
2. named-pipe startup and connection behavior may fail even though both projects compile
3. the current error-list implementation uses a compatibility-minded fallback and may need refinement after real runtime testing
4. there is still some line-ending churn in the worktree that should not be mixed into functional changes unless intentionally cleaned up

## Constraints To Preserve

Keep these constraints in place while getting MCP functional:

- no shell execution from the MCP server
- no direct file writes from the MCP server
- no filesystem access outside the solution root
- no autonomous agent loop
- preserve the boundary where VSIX owns Visual Studio APIs and MCP server owns tool registration and forwarding
- prefer minimal corrections over refactors

## What ChatGPT Should Focus On

Useful ChatGPT questions right now:

1. what is the minimum end-to-end validation plan for this bridge
2. what failure modes matter most at the MCP-server to pipe to VSIX boundary
3. what minimal observability should be added if connection debugging becomes difficult
4. which parts of the current shared abstractions are worth keeping stable during the connection phase

## Files To Read First

- `docs/CODING_STANDARDS.md`
- `docs/AI_COLLABORATION.md`
- `README.md`
- `VsMcpBridge.McpServer/Program.cs`
- `VsMcpBridge.McpServer/Pipe/PipeClient.cs`
- `VsMcpBridge.McpServer/Tools/VsTools.cs`
- `VsMcpBridge.Vsix/VsMcpBridgePackage.cs`
- `VsMcpBridge.Vsix/Pipe/PipeServer.cs`
- `VsMcpBridge.Vsix/Services/VsService.cs`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindow.cs`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindowControl.xaml`
- `VsMcpBridge.Vsix/ToolWindows/LogToolWindowControl.xaml.cs`

## Suggested Prompt

Use something close to this:

> Read `docs/CODING_STANDARDS.md`, `docs/AI_HANDOFF.md`, and `docs/AI_COLLABORATION.md` first. We are in the MCP connection phase. Assume the immediate goal is to get the VSIX host, named-pipe transport, and MCP server working end to end with the smallest correct changes. Review the current code and identify the next 3-5 concrete steps, the highest-risk boundary issues, and any minimum architectural guardrails we should preserve while making the bridge functional.

---

## GitHub Copilot - Notes

This section is reserved for short in-editor observations from pair-programming sessions with Bill.

---

## Codex - Notes

### Entry: 2026-04-10 - Build Stabilization And Phase Reset

Recent repository work completed:

- fixed the solution build
- fixed VSIX compile-time compatibility issues for `net472`
- reviewed and corrected the docs to align with the current VSIX command and tool-window scaffold
- updated the collaboration and handoff docs to reflect the current execution model

Current recommendation:

- do not expand the bridge yet
- validate real VSIX runtime behavior and one end-to-end MCP round trip next

Environmental note:

- interactive Visual Studio Experimental Instance validation still needs to be performed on a Windows machine with Visual Studio 2022
