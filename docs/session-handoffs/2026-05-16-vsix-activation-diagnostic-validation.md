# VSIX Activation Diagnostic Validation Handoff

## Summary

The inactive VSIX named-pipe path now has durable trace artifacts showing that VS-backed MCP tools fail with an operator-actionable activation diagnostic instead of an opaque timeout.

Observed trace:

- run name: `vsix-activation-diagnostic-trace-20260516`
- baseline commit: `cb0bc6a Improve VSIX activation diagnostics for pipe-backed MCP tools`
- path: MCP client -> `VsMcpBridge.McpServer` -> VS-backed MCP tool -> `PipeClient` -> named pipe
- inactive outcome: named pipe unavailable, structured activation diagnostic returned
- request id: `vsix-activation-diagnostic-trace-20260516-req-001`

Durable artifacts:

- `artifacts/logs/vsix-activation-diagnostic-trace-20260516.log`
- `artifacts/logs/vsix-activation-diagnostic-trace-20260516.metadata.json`
- `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`

## What Was Proven

- MCP server startup, initialize, and `tools/list` are not implicated by this diagnostic path.
- VS-backed tool invocation reaches `PipeClient`.
- `PipeClient` attempts a named-pipe connection with request/correlation metadata.
- when the VSIX named-pipe side is inactive, the returned tool result contains clear activation instructions.
- the diagnostic tells the operator to launch the Visual Studio Experimental Instance, open `View -> Other Windows -> VS MCP Bridge`, wait for tool-window initialization, and retry.
- request id metadata is preserved in the diagnostic response.
- raw payload and secret-like sentinels are not written into the durable trace artifacts.

## Expected Successful Activation Path

1. Launch the Visual Studio Experimental Instance for `Y:\vs-mcp-bridge`.
2. Open `View -> Other Windows -> VS MCP Bridge`.
3. Wait for the tool window to initialize the VSIX named-pipe side.
4. Retry a VS-backed MCP tool such as `vs_get_active_document` or `vs_list_solution_projects`.
5. `PipeClient` connects to `VsMcpBridge`, the VSIX dispatches the command to `VsService`, and the MCP server returns a successful tool result.

## Scope Exclusions

This artifact does not add MCP tools, retry loops, transport changes, VSIX startup changes, authentication, OAuth, sandboxing, or remote execution.

The goal is diagnostic clarity. It lets future sessions distinguish an inactive VSIX/named-pipe side from MCP server startup or tool discovery failure.

## Resume Guidance

When a VS-backed MCP tool reports `VS MCP Bridge is not active`, do not start by changing MCP transport. First confirm the VSIX activation path:

- Experimental Instance is running for this repo.
- `View -> Other Windows -> VS MCP Bridge` has been opened.
- the tool window has initialized.
- no orphaned `VsMcpBridge.McpServer` process is locking outputs.

If the named-pipe side is active and the same pipe-backed tool still fails, then inspect `%LocalAppData%\VsMcpBridge\Logs\McpServer\pipe-client.log` and `%LocalAppData%\VsMcpBridge\Logs\Vsix\pipe-server-trace.log` for the first missing boundary.
