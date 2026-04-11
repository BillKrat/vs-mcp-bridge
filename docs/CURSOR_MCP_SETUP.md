# Cursor MCP Setup

Last updated: 2026-04-10

## Purpose

This document explains the project-local Cursor setup for `vs-mcp-bridge`.

Goal:

- let Cursor launch the existing MCP server with the smallest possible setup
- avoid hand-editing per-machine Cursor settings for normal bring-up
- keep the runtime model aligned with the current repository architecture

## What Was Added

The repository now includes:

- `.cursor/mcp.json`

That file tells Cursor to launch the built `VsMcpBridge.McpServer` assembly as a local stdio MCP server.

Configured server name:

- `vs-mcp-bridge`

Launch target:

- `VsMcpBridge.McpServer/bin/Debug/net8.0/VsMcpBridge.McpServer.dll`

## Why It Uses The Built DLL

The config intentionally launches the built DLL with `dotnet` instead of using `dotnet run`.

Reason:

- this is a stdio MCP server
- `dotnet run` can introduce build/restore console output at process startup
- extra stdout chatter is risky for MCP transport

Using the built DLL keeps startup cleaner and makes Cursor reconnect behavior easier to interpret.

## How It Works

Runtime flow:

```text
Cursor
  -> MCP over stdio
VsMcpBridge.McpServer
  -> JSON over named pipe "VsMcpBridge"
VsMcpBridge.Vsix
  -> Visual Studio services and DTE
```

Boundary ownership:

- Cursor owns MCP client behavior
- `VsMcpBridge.McpServer` owns tool registration and forwarding
- `VsMcpBridge.Vsix` owns Visual Studio interaction

## One-Time Setup

### 1. Build the MCP server

From the repository root on Windows:

```powershell
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

That produces:

```text
VsMcpBridge.McpServer\bin\Debug\net8.0\VsMcpBridge.McpServer.dll
```

### 2. Open the repository in Cursor

Because `.cursor/mcp.json` is committed in the repo, Cursor should discover the project-local MCP server definition for this workspace.

### 3. Start the Visual Studio side

Run the VSIX in the Visual Studio Experimental Instance and let it finish loading.

What you want to see in the development Visual Studio output:

- `Initializing VS MCP Bridge package.`
- `Pipe server started on 'VsMcpBridge'.`
- `VS MCP Bridge package initialized.`

### 4. Confirm the Cursor MCP server is available

In Cursor, confirm the workspace MCP server named `vs-mcp-bridge` is enabled and that its tools are visible.

Expected tool names:

- `vs_get_active_document`
- `vs_get_selected_text`
- `vs_list_solution_projects`
- `vs_get_error_list`
- `vs_propose_text_edit`

## First Recommended Validation

Use one minimal read-only tool first:

- `vs_get_active_document`

Why this one:

- it is the simplest end-to-end proof that Cursor can call the MCP server, the MCP server can connect to the VSIX pipe, and the VSIX can answer a request

## What Success Looks Like

From Cursor:

- the MCP server stays connected
- the tool list is available
- `vs_get_active_document` returns document data instead of reconnecting or failing immediately

From the Visual Studio side:

- package load logs appear
- the pipe server has started
- when a tool runs, pipe-request logs should appear if connection tracing is enabled

## Troubleshooting

### Symptom: Cursor shows `Reconnecting 5/5`

Most likely meaning:

- the MCP server process is exiting during startup

Most likely causes:

- `VsMcpBridge.McpServer.dll` does not exist yet
- the repo was not built after pulling changes
- the workspace opened in Cursor is not the repo root that contains `.cursor/mcp.json`
- the launched command path does not match the build output

First fix:

```powershell
dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj
```

Then reopen or refresh the Cursor workspace MCP server.

### Symptom: Cursor sees the tools, but tool execution fails

Most likely meaning:

- MCP stdio is working
- the failure is now at the named-pipe or VSIX boundary

Check:

- the VSIX Experimental Instance is running
- the package loaded successfully
- the Visual Studio output contains `Pipe server started on 'VsMcpBridge'.`

### Symptom: The VSIX is running, but no tool call reaches it

Most likely meaning:

- Cursor is not actually calling the workspace MCP server you expect
- or the MCP server is running, but pipe connection is failing before the VSIX receives the request

Check both sides:

- Cursor MCP server status
- Visual Studio output
- MCP server debug output if available

## Notes For Developers

- The committed Cursor config is meant to reduce local setup friction, not replace runtime validation.
- It assumes the default `Debug/net8.0` output path for the MCP server.
- If the team later standardizes on `Release`, only the DLL path in `.cursor/mcp.json` should need to change.
- This setup does not change project architecture. It only provides a project-local way for Cursor to launch the already-existing MCP server.
