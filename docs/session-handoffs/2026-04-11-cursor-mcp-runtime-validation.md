# Session Handoff: Cursor MCP Runtime Validation

Date: 2026-04-11
Branch: `feature/approval-apply-ui-slice`
Latest runtime fix commit: `55fd4b9` (`Fix Cursor MCP runtime stability issues`)
Latest workflow/logging commit: `b931cbc` (`Persist MCP logging and session handoff workflow`)
Previous preservation commit: `1acaa36`

## Objective Reached

Validated the Cursor -> MCP server -> named pipe -> VSIX path for the read-only tool surface and preserved the runtime fixes in git.

## Validated Working Tools

- `vs_get_active_document`
- `vs_list_solution_projects`
- `vs_get_selected_text`
- `vs_get_error_list`
- `vs_propose_text_edit` (approval and apply path)

## Key Fixes Landed In `55fd4b9`

- [VsMcpBridge.McpServer/Program.cs](/Y:/vs-mcp-bridge/VsMcpBridge.McpServer/Program.cs)
  - cleared default logging providers to stop stdout pollution breaking stdio MCP JSON
- [VsMcpBridge.Shared/Services/PipeServer.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Shared/Services/PipeServer.cs)
  - fixed accepted-pipe handoff race
  - added durable VSIX-side pipe trace breadcrumbs
  - deferred `StreamWriter` creation until after request read
- [VsMcpBridge.McpServer/Pipe/PipeClient.cs](/Y:/vs-mcp-bridge/VsMcpBridge.McpServer/Pipe/PipeClient.cs)
  - added durable MCP-side pipe trace breadcrumbs
- [VsMcpBridge.Vsix/Services/VsService.cs](/Y:/vs-mcp-bridge/VsMcpBridge.Vsix/Services/VsService.cs)
  - replaced late-bound `dynamic` Error List access with typed `EnvDTE80.ErrorItems`

## Important Findings

- The original Cursor connection failure was caused by non-JSON log text reaching MCP stdout.
- The first named-pipe failure was a lambda capture race that passed `null` into `HandleConnectionAsync`.
- The later pipe hang was isolated to server-side writer creation timing; deferring writer creation unblocked request handling.
- The `vs_get_error_list` failure was a real runtime binder bug, not just transient VS state.

## Current State

- Runtime validation of the currently exposed MCP tools is complete.
- Proposal creation, approval, and apply have been validated successfully against a real file.
- The latest observed follow-on noise after apply was `JsonRpc Warning ... connection was lost`, but the proposal and apply both succeeded before that warning.

## Next Recommended Chunk

Decide whether to:
1. push the local commits, or
2. investigate post-apply Cursor/JsonRpc disconnect behavior as a separate stability pass.

Recommended next technical chunk if continuing runtime work:
1. reproduce the post-apply disconnect once
2. capture Cursor MCP output and VS logs around the disconnect
3. determine whether the connection loss is expected session teardown or a bridge-side bug

## Useful Runtime Logs

- MCP-side trace:
  - `C:\Users\billkratochvil\AppData\Local\VsMcpBridge\Logs\McpServer\pipe-client.log`
- VSIX-side trace:
  - `C:\Users\billkratochvil\AppData\Local\VsMcpBridge\Logs\Vsix\pipe-server-trace.log`
- Unhandled exceptions:
  - `C:\Users\billkratochvil\AppData\Local\VsMcpBridge\Logs\UnhandledExceptions`

## Restart Instructions For Next Session

- Read this handoff note first.
- Confirm branch is still `feature/approval-apply-ui-slice`.
- Start from the next chunk above using the normal gated workflow.
