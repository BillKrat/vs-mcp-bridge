# vs-mcp-bridge

VS MCP Bridge — a minimal C# solution that exposes Visual Studio IDE state to an AI model via the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/).

## Solution Structure

```
VsMcpBridge.slnx
├── VsMcpBridge.Shared/          # netstandard2.0 — strongly-typed request/response models
│   └── Models/
│       ├── PipeMessage.cs       # Named-pipe envelope & well-known command names
│       ├── VsRequests.cs        # One class per VS operation request
│       └── VsResponses.cs       # One class per VS operation response
│
├── VsMcpBridge.McpServer/       # net8.0 console app — local MCP server (stdio)
│   ├── Program.cs               # Host builder; registers MCP + pipe client
│   ├── Pipe/
│   │   └── PipeClient.cs        # Named-pipe client → VSIX
│   └── Tools/
│       └── VsTools.cs           # MCP tool definitions (5 tools)
│
└── VsMcpBridge.Vsix/            # net472 Visual Studio extension (Windows/VS only)
    ├── VsMcpBridgePackage.cs    # AsyncPackage entry point
    ├── source.extension.vsixmanifest
    ├── Pipe/
    │   └── PipeServer.cs        # Named-pipe server; dispatches to VsService
    ├── Services/
    │   └── VsService.cs         # DTE/error-list wrappers; diff generation
    └── ToolWindows/
        ├── LogToolWindow.cs          # Tool-window pane
        ├── LogToolWindowControl.xaml # WPF layout (log + approve/reject bar)
        └── LogToolWindowControl.xaml.cs
```

## Architecture

```
AI model (Codex / Claude / …)
      │  MCP over stdio
      ▼
VsMcpBridge.McpServer   (console app)
      │  JSON over named pipe "VsMcpBridge"
      ▼
VsMcpBridge.Vsix        (Visual Studio extension)
      │  DTE / VS SDK APIs
      ▼
Visual Studio IDE
```

**Responsibilities:**

| Layer | Responsibility |
|-------|----------------|
| McpServer | Receives MCP tool calls, validates inputs, forwards to VSIX via pipe |
| VSIX | Owns all VS interactions; only applies edits after user approval |
| Shared | Strongly-typed models shared by both layers |

## MCP Tools

| Tool | Description |
|------|-------------|
| `vs_get_active_document` | File path, language, and full text of the active editor |
| `vs_get_selected_text` | Currently selected text |
| `vs_list_solution_projects` | All projects in the open solution |
| `vs_get_error_list` | Errors, warnings, and messages from the Error List |
| `vs_propose_text_edit` | Returns a unified diff — **does not write files** |

## Constraints (by design)

- No file writes — all edits are proposed as diffs; the user approves in the VSIX tool window.
- No shell execution.
- No network calls beyond the MCP stdio transport.
- No autonomous/agent loops.

## Building

**McpServer + Shared** (cross-platform):
```bash
dotnet build VsMcpBridge.Shared/VsMcpBridge.Shared.csproj
dotnet build VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj
```

**VSIX** (Windows with Visual Studio 2022 + VSSDK workload):
```
Open VsMcpBridge.slnx in Visual Studio 2022 and build VsMcpBridge.Vsix.
```
