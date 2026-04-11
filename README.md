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

Developer note:

- the repository uses MVP/VM so the view stays passive, the viewmodel owns bindable state and commands, and the presenter coordinates workflow and service interaction; see [docs/MVPVM_OVERVIEW.md](docs/MVPVM_OVERVIEW.md) for the repo-specific pattern guide and examples

Current limitation:

- edit application rebuilds the full document from the generated diff, so preserving exact formatting and line endings still needs hardening

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

For the detailed living technical reference, see [docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md](docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md).
For a concise hand-off document intended for ChatGPT or another external reviewer, see [docs/CHATGPT_HANDOFF.md](docs/CHATGPT_HANDOFF.md).

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

The repository is in a stabilization and MCP-connection phase.

What is verified:

- the solution builds
- the VSIX project builds
- the shared and VSIX layers are separated cleanly in the source tree
- the VSIX source includes a command table, package bootstrap, and tool-window scaffold
- the bridge uses DI and interface-based services across the shared and VSIX layers

What is not yet fully verified from the current workspace:

- Visual Studio Experimental Instance load
- tool-window open behavior in real Visual Studio
- named-pipe startup during package load
- one full MCP-to-pipe-to-VSIX round-trip in a Windows Visual Studio environment

## Next Steps

The most important next work is runtime validation of the existing scaffold, in this order:

- verify VSIX Experimental Instance startup
- verify package load
- verify tool-window open
- verify named-pipe listener startup
- verify one read-only MCP round-trip end to end

After that, the strongest candidates are:

- better connection diagnostics
- bridge health and capabilities reporting
- request correlation and protocol versioning
- hardening the edit-application model

## Documentation Guidance

Use these docs together:

- `README.md`: quick orientation and build/test entry point
- `docs/MVPVM_OVERVIEW.md`: developer guide to the repository's MVP/VM split with concrete examples
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living architecture and roadmap document
- `docs/CHATGPT_HANDOFF.md`: current-state hand-off for external analysis and next-step planning
