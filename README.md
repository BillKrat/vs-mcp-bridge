# VS MCP Bridge

`vs-mcp-bridge` is a local integration that exposes selected Visual Studio IDE state to AI tooling through the Model Context Protocol (MCP).

The solution is split into two runtime processes plus a shared contract:

- `VsMcpBridge.McpServer`: a local MCP server that speaks stdio to an AI client
- `VsMcpBridge.Vsix`: a Visual Studio extension that runs inside the IDE
- `VsMcpBridge.Shared`: shared request/response and pipe contract models
- `VsMcpBridge.Vsix.Tests`: unit tests for the decoupled VSIX services and infrastructure

## What It Does Today

Current supported capabilities:

- read the active Visual Studio document
- read the current text selection
- list solution projects
- read the Visual Studio Error List
- propose text edits as diffs without directly writing files

The bridge is intentionally conservative at this stage:

- Visual Studio API access stays inside the VSIX
- the MCP server only talks to the VSIX over a local named pipe
- edits are proposed, not automatically applied
- diagnostics and unhandled exception capture are built in

The VSIX also includes a WPF tool window shell for bridge status and future approval UX:

- the view is now a passive WPF control
- tool window state is exposed through a viewmodel
- UI orchestration lives in a presenter using an MVPVM-style split
- bindings and commands use `CommunityToolkit.Mvvm`

Current limitation:

- the tool window architecture is in place, but runtime bridge events are not yet routed into the presenter end-to-end

## Solution Structure

```text
VsMcpBridge.slnx
|- VsMcpBridge.Shared/       shared contract models
|- VsMcpBridge.McpServer/    MCP stdio host and named-pipe client
|- VsMcpBridge.Vsix/         Visual Studio extension and named-pipe server
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

That document is the best place to understand:

- current architecture and control flow
- DI, diagnostics, and exception handling strategy
- testing posture
- known limitations and architectural gaps
- roadmap recommendations and future capabilities

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
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' .\VsMcpBridge.Vsix\VsMcpBridge.Vsix.csproj /restore /t:Build /p:Configuration=Debug
```

You can also open `VsMcpBridge.slnx` in Visual Studio and build `VsMcpBridge.Vsix` there.

## Test

```powershell
dotnet test .\VsMcpBridge.Vsix.Tests\VsMcpBridge.Vsix.Tests.csproj
```

## Current Status

The repository is now in a solid early-platform state:

- VSIX build, packaging, and install have been verified
- the extension loads in the Experimental instance
- the bridge uses DI and interface-based services
- the tool window now uses a passive view plus presenter/viewmodel split
- verbose diagnostics are in place
- unhandled exceptions are persisted through a swappable sink abstraction
- unit tests cover the current decoupled logic

## Next Steps

The most important next capability is wiring the edit proposal and approval flow into the tool window presenter, then completing the approval-and-apply workflow for proposed edits.

Other strong candidates are:

- bridge health and capabilities reporting
- request correlation and protocol versioning
- richer Visual Studio query tools
- SQLite-backed persistence for exception and approval history

## Documentation Guidance

Use these docs together:

- `README.md`: quick orientation and build/test entry point
- `docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`: living architecture and roadmap document
