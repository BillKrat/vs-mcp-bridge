# VS MCP Bridge

`vs-mcp-bridge` is a local integration that exposes selected IDE and workspace state to AI tooling through the Model Context Protocol (MCP).

The solution is split into host-specific runtimes plus shared infrastructure:

- `VsMcpBridge.McpServer`: a local MCP server that speaks stdio to an AI client
- `VsMcpBridge.Vsix`: a Visual Studio extension that runs inside the IDE
- `VsMcpBridge.App`: a standalone WPF host that demonstrates non-VSIX reuse of the bridge
- `VsMcpBridge.Shared`: shared contracts, abstractions, diagnostics, pipe dispatch, and tool-window orchestration
- `VsMcpBridge.Shared.Wpf`: reusable WPF views for the tool window UI
- `VsMcpBridge.Shared.Tests`: unit tests for the shared layer
- `VsMcpBridge.Vsix.Tests`: unit tests for VSIX-specific composition and service logic

## What It Does Today

Current supported capabilities:

- read the active Visual Studio document
- read the current text selection
- list solution projects
- read the Visual Studio Error List
- propose text edits as diffs
- route proposed edits into the tool window for approval or rejection
- apply approved edits inside Visual Studio through the VSIX host

The bridge is intentionally conservative at this stage:

- Visual Studio API access stays inside the VSIX
- the MCP server only talks to the VSIX over a local named pipe
- edits still require explicit approval in the tool window before they are applied
- diagnostics and unhandled exception capture are built in
- shared bridge infrastructure is now decoupled from the VSIX so other hosts can provide their own implementations

The standalone app now serves two purposes:

- it is a reference host for developers who want to use the bridge outside a VSIX
- it validates that host-specific code stays out of the VSIX when it belongs in shared or in an alternate host

Host behavior today:

- `VsMcpBridge.Vsix` provides Visual Studio-backed behavior through DTE and Visual Studio services
- `VsMcpBridge.App` provides workspace/file-system-backed behavior while reusing the same shared presenter, viewmodel, pipe server, approval workflow, and WPF view

The VSIX includes a WPF tool window for bridge status and approval UX:

- the view lives in `VsMcpBridge.Shared.Wpf` as a passive WPF control
- tool window state is exposed through a viewmodel
- UI orchestration lives in a presenter using an MVPVM-style split
- bindings and commands use `CommunityToolkit.Mvvm`
- `ProposalFilePath` can be entered manually or selected with `Browse`
- `Browse` only sets `ProposalFilePath`; the existing load workflow still populates both panes
- the original pane stays read-only, while the proposed pane is editable before submit and read-only while approval is pending
- `Submit Proposal` stays disabled until the file loads successfully and the proposed text differs from the loaded original
- approve or reject clicks do not immediately reset the proposal UI; terminal outcomes drive the reset instead
- after a terminal outcome, pending approval state is cleared, completed proposal callbacks cannot be reused, and proposal-entry state is refreshed from `ProposalFilePath`
- apply failures are surfaced in the tool window as concise status text in addition to the bridge logs
- the terminal status message remains visible in the tool window after the proposal cycle completes

Developer note:

- the repository uses MVP/VM so the view stays passive, the viewmodel owns bindable state and commands, and the presenter coordinates workflow and service interaction; see [docs/MVPVM_OVERVIEW.md](docs/MVPVM_OVERVIEW.md) for the repo-specific pattern guide and examples

Current limitation:

- new proposals may carry one or more single-file `RangeEdit` entries in addition to `Diff`
- the unified diff remains the preview format shown to the operator
- apply prefers range-based replacement when `RangeEdit`/`RangeEdits` metadata is present, and falls back to full-document apply when range metadata is absent
- single-file multi-range apply is all-or-nothing across the full range set
- ambiguity or drift in any intended range fails the entire apply explicitly instead of guessing or partially applying
- live manual validation should focus on multi-range success, drift failure after submit and before approve, and adjacent/nearby range behavior; ambiguity failure is primarily an automated safety proof

## Solution Structure

```text
VsMcpBridge.slnx
|- VsMcpBridge.Shared/       shared contracts, services, presenter/viewmodel, diagnostics abstractions
|- VsMcpBridge.Shared.Wpf/   shared WPF tool-window views
|- VsMcpBridge.Shared.Tests/ unit tests for shared logic
|- VsMcpBridge.McpServer/    MCP stdio host and named-pipe client
|- VsMcpBridge.App/          standalone WPF host and non-VSIX service implementations
|- VsMcpBridge.Vsix/         Visual Studio extension and Visual Studio-specific implementations
`- VsMcpBridge.Vsix.Tests/   unit tests for VSIX infrastructure and logic
```

## Architecture

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

For current system behavior and request flow, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
For the detailed living technical reference and roadmap, see [docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md](docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md).

Blog series:

- [VS MCP Bridge Blog Series: Part 1](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-1)
- [VS MCP Bridge Blog Series: Part 2](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-2)
- [VS MCP Bridge Blog Series: Part 3](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-3)
- [VS MCP Bridge Blog Series: Part 4](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-4)
- [VS MCP Bridge Blog Series: Part 5](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-5)
- [VS MCP Bridge Blog Series: Part 6](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-6)
- [VS MCP Bridge Blog Series: Part 7](http://adventuresontheedge.net/post/2026/04/11/vs-mcp-bridge-blog-series-part-7)

## Build

### MCP Server And Shared

```powershell
dotnet build .\VsMcpBridge.Shared\VsMcpBridge.Shared.csproj
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

### VSIX

The VSIX project targets Visual Studio 2022 on Windows and requires the Visual Studio SDK tooling/workload.
The project also references `CommunityToolkit.Mvvm` for tool window bindings and commands.

From a Developer PowerShell or Visual Studio MSBuild environment:

```powershell
.\scripts\build-vsix.ps1 -Restore
```

You can also open `VsMcpBridge.slnx` in Visual Studio and build `VsMcpBridge.Vsix` there.

### Standalone App

The standalone app can be built with the normal .NET SDK:

```powershell
dotnet build .\VsMcpBridge.App\VsMcpBridge.App.csproj
```

## Test

Shared layer:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj
```

VSIX-facing tests:

```powershell
.\scripts\build-vsix.ps1 -Restore
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll
```

The build script probes these MSBuild locations in order and uses the first one found:

```text
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\arm64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\amd64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe
C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe
```

To build a different project with the same toolchain:

```powershell
.\scripts\build-vsix.ps1 -Restore -Project 'VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj'
& 'C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' .\VsMcpBridge.Vsix.Tests\bin\Debug\net472\VsMcpBridge.Vsix.Tests.dll
```

Important build note:

- `dotnet test .\VsMcpBridge.slnx` is not the correct top-level runner for the repo because the legacy VSIX project depends on Visual Studio MSBuild/VSSDK tooling rather than the SDK-hosted `dotnet` MSBuild path.

## Current Status

The repository is now past initial MCP bring-up and through end-to-end runtime validation for the current tool surface.

What is verified:

- the solution builds
- the VSIX project builds
- the shared and VSIX layers are separated cleanly in the source tree
- the VSIX source includes a command table, package bootstrap, and tool-window scaffold
- the bridge uses DI and interface-based services across the shared and VSIX layers
- the Visual Studio Experimental Instance loads the VSIX successfully
- the named-pipe listener starts during package load
- Cursor can connect to the project-local MCP server through `.cursor/mcp.json`
- the current read-only MCP tools work end to end:
  - `vs_get_active_document`
  - `vs_get_selected_text`
  - `vs_list_solution_projects`
  - `vs_get_error_list`
- `vs_propose_text_edit` works through proposal, approval, and apply
- post-apply connectivity was verified with a successful follow-up `vs_get_active_document` call

Observed runtime note:

- `JsonRpc Warning: No target methods are registered that match "NotificationReceived"` was observed after apply, but it did not block the bridge or subsequent tool calls

## Next Steps

The most important next work is no longer first-time runtime validation. The best next targets are:

- add or expand targeted automated coverage around the MCP server and pipe boundary
- harden the edit-application model so formatting and line endings are preserved more reliably
- improve protocol/connection diagnostics without polluting stdio MCP transport
- investigate non-blocking post-apply notification noise only if it becomes actionable

## Documentation Guidance

Use these docs together:

- `README.md`: quick orientation and build/test entry point
- `docs/ARCHITECTURE.md`: single source of truth for current system behavior
- `docs/gated_turn-based_workflow-Codex.txt`: Bill and Codex collaboration workflow for gated execution
- `docs/MVPVM_OVERVIEW.md`: developer guide to the repository's MVP/VM split with concrete examples
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living architecture and roadmap document
