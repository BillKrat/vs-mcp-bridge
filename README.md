# VS MCP Bridge

`vs-mcp-bridge` is a local integration that exposes selected Visual Studio IDE state to AI tooling through the Model Context Protocol (MCP).

The solution is split into two runtime processes plus shared infrastructure:

- `VsMcpBridge.McpServer`: a local MCP server that speaks stdio to an AI client
- `VsMcpBridge.Vsix`: a Visual Studio extension that runs inside the IDE
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
VsMcpBridge.Vsix
  -> Visual Studio SDK / DTE APIs
Visual Studio IDE
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

The repository is now in a solid early-platform state:

- VSIX build, packaging, and install have been verified
- the extension loads in the Experimental instance
- the bridge uses DI and interface-based services across the shared and VSIX layers
- shared infrastructure is no longer coupled to the VSIX project
- the tool window uses a passive shared-WPF view plus presenter/viewmodel split
- proposal submission, approval, rejection, and apply are wired end-to-end through the tool window
- verbose diagnostics are in place
- unhandled exceptions are persisted through a swappable sink abstraction
- unit tests cover both the shared layer and the VSIX-specific layer

## Next Steps

The most important next capability is hardening the edit-application model so approved edits preserve exact file content details instead of rebuilding the entire document from a display-oriented diff.

Other strong candidates are:

- bridge health and capabilities reporting
- request correlation and protocol versioning
- richer Visual Studio query tools
- durable persistence for exception and approval history

## Documentation Guidance

Use these docs together:

- `README.md`: quick orientation and build/test entry point
- `docs/MVPVM_OVERVIEW.md`: developer guide to the repository's MVP/VM split with concrete examples
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living architecture and roadmap document
- `docs/CHATGPT_HANDOFF.md`: current-state hand-off for external analysis and next-step planning
