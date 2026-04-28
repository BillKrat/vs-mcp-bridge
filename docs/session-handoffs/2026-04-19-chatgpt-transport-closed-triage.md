# ChatGpt Transport Closed Triage

Status: ACTIVE

Date: 2026-04-19
Branch: `feature/approval-apply-ui-slice`
Purpose: capture the current ChatGpt/Codex MCP failure mode, the triage result, and the local remediation applied so the next session does not need to rediscover it.

## Symptom

- Bridge tools that had previously worked started failing again.
- Live probes for:
  - `vs_get_active_document`
  - `vs_list_solution_projects`
  - `vs_get_error_list`
  all failed immediately with `Transport closed`.
- The Visual Studio Experimental instance was running, but no new bridge-log traffic appeared.

## What Was Confirmed

- Visual Studio 2026 Insiders Experimental was running:
  - `C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe`
- The VSIX package did load in Experimental and the named-pipe server did start.
- `ActivityLog.xml` showed:
  - `Initializing VS MCP Bridge package.`
  - `Pipe server started on 'VsMcpBridge'.`
  - `VS MCP Bridge package initialized.`
- The MCP-side AppData logs were stale and had no entries for the failing attempt.
- No `VsMcpBridge.McpServer` process was running at the time of the `Transport closed` failure.

## Exact Triage Result

This failure was not the earlier VSIX proposal marshalling bug and not a named-pipe dispatch problem.

The failure occurred earlier, at the ChatGpt/Codex MCP stdio transport boundary:

- the VSIX side was alive
- the bridge pipe listener was available
- but the MCP server process backing the `vs_mcp_bridge` tools was not alive for the current tool call
- tool invocation therefore failed before any named-pipe request was sent

In short:

- exact failing stage: MCP server transport/session startup or retention
- not failing stage: VSIX package initialization
- not failing stage: pipe server request handling
- not failing stage: proposal creation or approval UI

## Local Risk Identified

The local Codex MCP configuration was still using:

```toml
[mcp_servers.vs_mcp_bridge]
command = "dotnet"
args = ["run", "--project", "Y:\\vs-mcp-bridge\\VsMcpBridge.McpServer\\VsMcpBridge.McpServer.csproj"]
```

That launch mode is fragile for stdio MCP transport because:

- `dotnet run` can emit build or restore output before the server is fully serving MCP over stdio
- it depends on a mapped drive path (`Y:\`) that may not be stable in every host launch context even if it works in an interactive shell
- it adds an avoidable build/launch step to every MCP session

## Local Remediation Applied

Updated local Codex config at:

- `C:\Users\billkratochvil\.codex\config.toml`

New launch configuration:

```toml
[mcp_servers.vs_mcp_bridge]
command = "dotnet"
args = ["\\\\Mac\\Dev\\vs-mcp-bridge\\VsMcpBridge.McpServer\\bin\\Debug\\net8.0\\VsMcpBridge.McpServer.dll"]
```

Why this is safer:

- launches the already-built MCP server artifact directly
- avoids `dotnet run` build chatter on stdio
- uses the same UNC-rooted workspace path as the active repo session

## Validation Performed

- `dotnet build VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj` succeeded
- the built artifact exists at:
  - `\\Mac\Dev\vs-mcp-bridge\VsMcpBridge.McpServer\bin\Debug\net8.0\VsMcpBridge.McpServer.dll`
- manual launch test succeeded:
  - `dotnet \\Mac\Dev\vs-mcp-bridge\VsMcpBridge.McpServer\bin\Debug\net8.0\VsMcpBridge.McpServer.dll`
  - the process stayed alive until explicitly stopped

## Important Limitation

Changing the local MCP config does not revive an already-closed MCP transport inside the current assistant session.

If bridge tools still show `Transport closed`, the host session needs to reload the MCP server using the updated config path.

## Next Session Guidance

- start from this handoff and `docs/session-handoffs/2026-04-20-codex-round-trip-proposal-unblocked.md`
- assume the bridge is healthy on the VSIX side unless new logs contradict that
- if `Transport closed` appears again, verify first whether the MCP server process is running before inspecting the VSIX named-pipe path
- prefer direct built-artifact launch for MCP stdio transport over `dotnet run`
