# Logging Diagnostic Runbook (UI + StdErr)

Use this runbook to localize MCP/chat-engine hangs with the new provider-based logging.

## Scope

- validates provider switching (`Debug` vs `StdErr`)
- confirms UI log surfacing through MVP/VM
- captures correlation points across MCP request flow

## 1) App host validation

1. Set `VsMcpBridge.App/appsettings.json`:
   - `VsMcpBridge:Logging:Provider = StdErr`
   - `VsMcpBridge:Logging:MinimumLevel = Trace`
2. Start `VsMcpBridge.App`.
3. Trigger a known request path from the UI (for example: "what is the active file").
4. Verify both:
   - log lines appear in the App UI log panel
   - stderr receives matching timestamped lines

Chat/OpenAI validation using the existing App request box:

- `ping` with `Adventures:ChatEngine:Provider=Fake` should return `pong`
- `ping` with `Adventures:ChatEngine:Provider=OpenAI` and `UseRealApi=false` should return the OpenAI stub response
- with `UseRealApi=true`, verify OpenAI request completion or non-success status with sanitized response summary in logs

Expected evidence:
- startup messages from presenter/service initialization
- request start + request completion lines

## 2) VSIX host validation

Set environment variables before launching Experimental Instance:

- `VSMCPBRIDGE_VsMcpBridge__Logging__Provider=StdErr`
- `VSMCPBRIDGE_VsMcpBridge__Logging__MinimumLevel=Trace`

Then:
1. Launch VS Experimental Instance.
2. Open the VS MCP Bridge tool window.
3. Trigger one read-only MCP call and one proposal flow.
4. Verify:
   - tool window log text updates in real time
   - provider output lines include request/proposal correlation data

## 3) MCP hang localization checklist

When reproducing ping/pong or chat startup hangs, capture the last observed stage:

1. `McpServer` request entry
2. `PipeClient` connect attempt
3. `PipeClient` request write complete
4. `PipeServer` request received
5. `VsService` operation start
6. `VsService` operation completion
7. `PipeServer` response write complete
8. `PipeClient` response read complete
9. `McpServer` tool response completion

If execution stops between two stages, treat that gap as the first actionable boundary.

### Quick triage markers (in order)

Use this compact sequence during incident triage. The first missing marker usually identifies the failing boundary.

1. `MCP chat_engine_ping started` or `MCP chat_engine_chat started`
2. `Pipe request started`
3. `Dispatching pipe command`
4. `Running VS service operation '<operation>'`
5. `VS service operation '<operation>' completed`
6. `Pipe command '<command>' completed`
7. `Pipe request completed`
8. `MCP chat_engine_ping completed` or `MCP chat_engine_chat completed`

## 4) Artifacts to collect

- App UI log text (copy from tool window)
- stderr capture from host process
- `%LocalAppData%\VsMcpBridge\Logs\McpServer\pipe-client.log`
- `%LocalAppData%\VsMcpBridge\Logs\Vsix\pipe-server-trace.log`

Environment/config snapshot to capture with artifacts:

- `VsMcpBridge:Logging:*`
- `Adventures:ChatEngine:Provider`
- `Adventures:ChatEngine:OpenAI:UseRealApi`
- whether `Adventures:ChatEngine:OpenAI:ApiKey` and `Adventures:ChatEngine:OpenAI:Model` are populated (do not paste secrets)

## 5) Fast rollback

After diagnostics, set provider back to default:

- App `Provider = Debug`
- clear VSIX environment override variables
