# VS MCP Bridge Technical Analysis

Last updated: 2026-04-10

## Purpose

This document is the living technical reference for `vs-mcp-bridge`.

Its job is to stay grounded in the repository as it exists now, not in an aspirational future architecture.

For fast current-state onboarding, read `docs/AI_HANDOFF.md` first.

## Executive Summary

`vs-mcp-bridge` is a local bridge between an MCP client and a host that can expose IDE or workspace state.

Current host shape:

- `VsMcpBridge.McpServer` is the MCP stdio process
- `VsMcpBridge.Vsix` is the Visual Studio host
- `VsMcpBridge.App` is a standalone WPF host for shared bridge behavior
- `VsMcpBridge.Shared` and `VsMcpBridge.Shared.Wpf` hold the common contracts and UI pieces

The repository is currently in a connection-first phase.

That means the highest-value work is:

1. verify the VSIX loads
2. verify the named-pipe bridge starts and accepts requests
3. verify the MCP server can talk to the host end to end
4. only then harden or broaden behavior

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

Not yet fully verified in this environment:

- real Experimental Instance load
- real tool-window open behavior
- real MCP-to-pipe-to-VSIX round trip

## Current Technical Priorities

The next phase should optimize for runtime proof, not architecture expansion.

Priority order:

1. verify package load in Experimental Instance
2. verify tool-window creation
3. verify named-pipe server startup
4. verify one read-only tool call end to end
5. add only the minimum diagnostics needed to debug connection failures

## Known Risk Areas

1. Build success currently exceeds runtime validation.
2. Pipe startup and connection behavior are likely to be the first real runtime failure point.
3. The current diagnostics and logging may be too thin for rapid connection debugging.
4. The current Error List path is compile-safe but still needs runtime confirmation in real Visual Studio.

## Guidance For Future Changes

During the connection-first phase:

- prefer small changes over refactors
- fix runtime blockers before improving architecture elegance
- do not add capabilities just because the bridge could support them later
- keep docs aligned with verified reality

After the bridge is proven end to end, the likely next technical topics are:

- better connection diagnostics
- clearer protocol/version metadata
- stronger structured edit models beneath the diff preview
- hardening the approval/apply workflow

## Related Documents

- `docs/CODING_STANDARDS.md`
- `docs/AI_COLLABORATION.md`
- `docs/AI_HANDOFF.md`
- `docs/MVPVM_OVERVIEW.md`
- `README.md`
