# VS MCP Bridge Technical Analysis

Last updated: 2026-04-11

## Purpose

This document is the living technical reference for `vs-mcp-bridge`.

Its job is to stay grounded in the repository as it exists now, not in an aspirational future architecture.

For fast onboarding, start with `README.md` and `docs/gated_turn-based_workflow-Codex.txt`.

## Executive Summary

`vs-mcp-bridge` is a local bridge between an MCP client and a host that can expose IDE or workspace state.

Current host shape:

- `VsMcpBridge.McpServer` is the MCP stdio process
- `VsMcpBridge.Vsix` is the Visual Studio host
- `VsMcpBridge.App` is a standalone WPF host for shared bridge behavior
- `VsMcpBridge.Shared` and `VsMcpBridge.Shared.Wpf` hold the common contracts and UI pieces

The repository has now completed connection-first bring-up for the currently exposed tool slice.

That means the highest-value work has shifted from first proof to selective hardening:

1. preserve and document the validated behavior
2. add automated coverage for the stdio and named-pipe boundaries
3. harden edit application and diagnostics
4. broaden capabilities only after the validated baseline remains stable

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

Key files:

- `VsMcpBridgePackage.cs`
- `Pipe/PipeServer.cs`
- `Services/VsService.cs`
- `ToolWindows/LogToolWindow.cs`
- `ToolWindows/LogToolWindowControl.xaml`

### `VsMcpBridge.App`

Purpose:

- exercises shared bridge pieces outside the VSIX host
- serves as a secondary host example

Current phase note:

- this project is useful, but it is not the primary focus until the VSIX plus MCP path is working reliably

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
- no direct file writes from the MCP server
- no filesystem access outside the solution root
- no autonomous agent loop
- edits remain proposal-based rather than silently applied
- Visual Studio API access stays in the VSIX host

## Current Verified State

Verified recently:

- the solution builds
- the VSIX project builds
- the VSIX project has the minimum WPF and JSON support needed for the current scaffold
- the package no longer references a missing menu resource
- real Experimental Instance load
- named-pipe listener startup during package load
- real MCP-to-pipe-to-VSIX round trips for the current tool surface
- successful proposal creation, approval, and apply through `vs_propose_text_edit`
- post-apply connectivity via a follow-up successful read-only tool call

## Current Technical Priorities

The next phase should optimize for stability and coverage, not initial runtime proof.

Priority order:

1. add automated coverage for stdio MCP startup and named-pipe request flow
2. harden proposal/apply behavior, especially document formatting and line-ending preservation
3. keep diagnostics strong without reintroducing stdout pollution
4. expand capability only after the current validated slice remains stable

## Known Risk Areas

1. The bridge now has a validated runtime path, but automated regression coverage for the MCP and pipe boundaries is still thin.
2. Edit application still rebuilds the full document and may need hardening around formatting and line endings.
3. Diagnostics must remain file/debug-based for MCP host work so stdio JSON transport stays clean.
4. Non-blocking `NotificationReceived` JsonRpc warnings were observed after apply and may deserve investigation only if they become user-visible or disruptive.

## Guidance For Future Changes

During the current stabilization phase:

- prefer small changes over refactors
- preserve the validated runtime baseline before broadening behavior
- fix concrete regressions before improving architecture elegance
- do not add capabilities just because the bridge could support them later
- keep docs aligned with verified reality

After the validated baseline is better covered, the likely next technical topics are:

- stronger automated tests around MCP startup and pipe flow
- clearer protocol/version metadata
- stronger structured edit models beneath the diff preview
- continued hardening of the approval/apply workflow

## Related Documents

- `README.md`
- `docs/gated_turn-based_workflow-Codex.txt`
- `docs/CODING_STANDARDS.md`
- `docs/MVPVM_OVERVIEW.md`
