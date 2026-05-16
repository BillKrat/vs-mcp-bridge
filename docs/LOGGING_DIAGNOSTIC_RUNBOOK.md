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

Repeatable App-host ping workflow:

- use `docs/app-host-ping-trace-workflow.md` when you need a durable AI-runnable procedure for launching the App host, exercising `ping`, capturing correlated logs, generating a Mermaid sequence diagram, and comparing the observed flow against code
- store dated artifacts under `artifacts/logs/`, `docs/diagrams/`, and `docs/blog-drafts/` rather than overwriting prior runs
- keep App-host observations separate from inferred or separately observed VSIX-host paths

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
3. Trigger `ping` in the request input surface and verify response behavior for the active chat provider mode.
4. Trigger one read-only MCP call and one proposal flow.
5. Verify:
   - tool window log text updates in real time
   - provider output lines include request/proposal correlation data

Repeatable VSIX-host ping workflow:

- use `docs/vsix-host-ping-trace-workflow.md` when you need a durable AI-runnable procedure for launching the Experimental Instance, exercising `ping`, capturing correlated tool-window evidence, generating a Mermaid sequence diagram, and comparing the observed flow against code
- store dated artifacts under `artifacts/logs/`, `docs/diagrams/`, and `docs/blog-drafts/` rather than overwriting prior runs
- record the effective provider path observed inside the Experimental Instance instead of assuming it from prior documentation

Repeatable VSIX-host selected-text workflow:

- use `docs/vsix-host-selected-text-trace-workflow.md` when you need a durable AI-runnable procedure for validating the prompt-box `what is the selected text` route against a real editor selection
- this workflow validates the built-in presenter-to-VS-service path, not the MCP `vs_get_selected_text` tool path
- store dated artifacts under `artifacts/logs/`, `docs/diagrams/`, and `docs/blog-drafts/` rather than overwriting prior runs
- record the selected file, selected text sentinel, and whether the interaction was manual or automated

## 3) Bridge tool execution validation

Use `docs/tool-execution-trace-workflow.md` when you need a durable AI-runnable procedure for validating the shared bridge tool catalog/executor path.

The current baseline validates:

- compiled catalog discovery for `bridge.regexTextSearch`
- default discovery through `CompiledBridgeToolDiscovery`
- `BridgeToolExecutor` start/completion logging
- request id and operation id preservation
- structured `BridgeToolResult` output
- policy evaluation before tool invocation
- redacted Trace-level request/result payload logging
- terminal `BridgeAuditEnvelope` emission when an audit sink is registered

Security-aware compiled-tool artifacts:

- `artifacts/logs/tool-security-trace-20260509.log`
- `artifacts/logs/tool-security-trace-20260509.metadata.json`
- `docs/diagrams/tool-security-trace-20260509.mmd`
- `docs/session-handoffs/2026-05-09-tool-security-validation.md`

This workflow intentionally does not validate enabled MEF directory discovery, directory-loaded plugins as a production plugin model, BM25, MCP transport, presenter behavior, proposal behavior, or VSIX host behavior.

When validating the optional MEF discovery seam separately, capture these boundaries:

- `MEF bridge tool discovery started`
- missing-directory warnings when configured directories do not exist
- assembly-load warnings for invalid or unloadable files
- `MEF bridge tool discovery completed`

MEF discovery logs must remain discovery-only evidence. Tool execution, policy enforcement, payload redaction, audit emission, and correlation preservation still belong to `BridgeToolExecutor`.

## 4) End-to-end automation and Mermaid evidence

For any workflow that matters to development or triage, the expected standard is:

1. run the workflow end-to-end through repeatable automation, or through a documented manual trigger with automated capture steps
2. capture correlated logs and relevant host output
3. generate a Mermaid sequence diagram from the observed evidence
4. compare the diagram against the intended code path
5. treat the first missing or failed marker as the actionable boundary

New code should use the existing boundary logging pattern when applicable:

- create or propagate a request/correlation id
- log operation start and completion/failure
- include operation name, request id, success/failure state, and elapsed timing where useful
- keep MCP stdout clean and route diagnostics through UI log, file log, Debug, or StdErr as appropriate
- send tool execution payload-oriented log fields through `ISecurityRedactor`; do not write raw secrets, tokens, passwords, bearer values, prompts containing secrets, exception dumps containing secrets, or trace artifacts containing secrets

Do not add opaque workflow paths. If a future AI session cannot quickly answer "where did this request stop?", add the minimum logging or artifact capture needed before expanding that path.

## 5) MCP hang localization checklist

Important scope note:

- `chat_engine_ping` validates the MCP host chat-engine path but does not traverse the named-pipe bridge into the VSIX host
- when you need to prove MCP -> pipe -> VSIX transport, use a pipe-backed tool such as `vs_get_active_document`, `vs_get_selected_text`, `vs_list_solution_projects`, `vs_get_error_list`, or a proposal tool

### Visual Studio Copilot MCP activation

For Visual Studio Insiders Copilot Agent validation, activate the bridge from the Experimental Instance that hosts the VSIX:

1. start the Experimental Instance for `Y:\vs-mcp-bridge`
2. open `View -> Other Windows -> VS MCP Bridge` to initialize the VSIX and named-pipe server
3. open a real editor document
4. open GitHub Copilot Chat in the same Experimental Instance
5. enable/select the `vs-mcp-bridge` tools in Agent mode
6. allow the bridge tools for the current session when prompted
7. invoke `vs_get_active_document` and then `vs_list_solution_projects`

Visual Studio 2026 Insiders may show MCP trust/tool confirmation dialogs. Session-only approval is sufficient for triage. If activation fails once and then builds start failing with locked files under `VsMcpBridge.McpServer\bin\Debug\net10.0`, check for an orphaned server:

```powershell
Get-Process VsMcpBridge.McpServer -ErrorAction SilentlyContinue
```

Stop only the orphaned `VsMcpBridge.McpServer` process, then rebuild the MCP server project.

As of the 2026-05-16 triage run, `scripts\validate-mcp.ps1` can hang while building its temporary helper before MCP initialization. A direct MCP stdio validation still succeeded: initialize returned `VsMcpBridge.McpServer 1.0.0.0`, `tools/list` returned 16 tools, and `chat_engine_ping` returned `pong`. Treat that script hang as a validation-helper issue unless direct MCP activation also fails.

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

## 6) Artifacts to collect

- App UI log text (copy from tool window)
- stderr capture from host process
- `%LocalAppData%\VsMcpBridge\Logs\McpServer\pipe-client.log`
- `%LocalAppData%\VsMcpBridge\Logs\Vsix\pipe-server-trace.log`

Environment/config snapshot to capture with artifacts:

- `VsMcpBridge:Logging:*`
- `Adventures:ChatEngine:Provider`
- `Adventures:ChatEngine:OpenAI:UseRealApi`
- whether `Adventures:ChatEngine:OpenAI:ApiKey` and `Adventures:ChatEngine:OpenAI:Model` are populated (do not paste secrets)
- whether security redaction/policy/audit defaults were overridden for the run; do not paste policy inputs or audit metadata that contains unredacted sensitive values

## 7) Fast rollback

After diagnostics, set provider back to default:

- App `Provider = Debug`
- clear VSIX environment override variables
