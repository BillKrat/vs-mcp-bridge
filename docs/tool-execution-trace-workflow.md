# Bridge Tool Execution Trace Workflow

Use this workflow to repeat the observed bridge tool execution validation, capture durable artifacts, and compare the resulting sequence against the current shared tool code.

## Purpose

Provide a repeatable AI-friendly and developer-friendly process for:

- invoking a compiled bridge tool through `IBridgeToolExecutor`
- proving catalog discovery through `CompiledBridgeToolCatalog`
- proving the minimal policy/redaction/audit seams around execution
- collecting correlated execution-boundary logs
- preserving request and operation correlation IDs
- generating a Mermaid sequence diagram from observed behavior
- producing durable artifacts future sessions can use for triage before expanding into MEF, plugin loading, or search ranking

## Scope

This workflow documents the shared compiled bridge tool path only.

It does not validate:

- MEF discovery
- directory-loaded plugins
- BM25 or ranked search
- MCP stdio transport
- named-pipe transport
- presenter, proposal, or Visual Studio service behavior

## Observed Baseline Run

This workflow was validated with a temporary console harness that used the production shared DI/tool services:

- run name: `tool-regex-search-trace-20260509`
- branch: `main`
- commit: `85401fd`
- catalog: `CompiledBridgeToolCatalog`
- executor: `BridgeToolExecutor`
- policy: `AllowToolExecutionPolicy`
- redactor: `BridgeSecurityRedactor`
- audit sink: `NoOpAuditSink`
- tool: `bridge.regexTextSearch`
- request id: `tool-trace-20260509-req-001`
- operation id: `tool-regex-search-20260509-op-001`
- pattern: `error|warning`
- case sensitive: `false`
- max results: `10`
- observed result: success, 2 returned matches, 2 total matches

Reference artifacts:

- sequence diagram: [`docs/diagrams/tool-regex-search-trace-20260509.mmd`](diagrams/tool-regex-search-trace-20260509.mmd)
- observed log transcript: [`artifacts/logs/tool-regex-search-trace-20260509.log`](../artifacts/logs/tool-regex-search-trace-20260509.log)
- run metadata: [`artifacts/logs/tool-regex-search-trace-20260509.metadata.json`](../artifacts/logs/tool-regex-search-trace-20260509.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-09-tool-execution-validation.md`](session-handoffs/2026-05-09-tool-execution-validation.md)

## Observed Security-Aware Run

After the foundational security seams were added, the compiled regex text-search path was revalidated with secret-like inputs and an in-memory audit sink:

- run name: `tool-security-trace-20260509`
- branch: `main`
- commit: `aa9a849`
- catalog: `CompiledBridgeToolCatalog`
- executor: `BridgeToolExecutor`
- policy: trace-only `RecordingAllowPolicy`
- redactor: `BridgeSecurityRedactor`
- audit sink: `InMemoryAuditSink`
- tool: `bridge.regexTextSearch`
- request id: `tool-security-trace-20260509-req-001`
- operation id: `tool-security-trace-20260509-op-001`
- pattern: `warning|error|authorization|password|token|apiKey`
- observed result: success, 10 returned matches, 10 total matches

Security-aware reference artifacts:

- sequence diagram: [`docs/diagrams/tool-security-trace-20260509.mmd`](diagrams/tool-security-trace-20260509.mmd)
- observed log transcript: [`artifacts/logs/tool-security-trace-20260509.log`](../artifacts/logs/tool-security-trace-20260509.log)
- run metadata: [`artifacts/logs/tool-security-trace-20260509.metadata.json`](../artifacts/logs/tool-security-trace-20260509.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-09-tool-security-validation.md`](session-handoffs/2026-05-09-tool-security-validation.md)

This run intentionally used secret-like `apiKey`, `token`, `password`, and bearer authorization inputs. Durable artifacts store only redacted payload evidence and must not contain raw secret values.

## Preconditions

- repository root: `Y:\vs-mcp-bridge`
- branch and commit should be recorded before the run
- current shared tests should pass:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj
```

- tool execution should use `AddBridgeToolServices()` so the catalog/executor path matches shared composition
- the harness may override `IAuditSink` with `InMemoryAuditSink` when audit envelope assertions are part of the run
- use a deterministic request id and operation id in the trace harness

## Run Procedure

### 1. Create a small trace harness

Use a temporary console app outside the repository, or an equivalent test harness, that references:

- `VsMcpBridge.Shared`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `VsMcpBridge.Shared.Composition`
- `VsMcpBridge.Shared.Loggers`
- `VsMcpBridge.Shared.Tools`

The harness should:

1. create a `RecordingBridgeLogger`
2. register it as `ILogger`
3. call `services.AddBridgeToolServices()`
4. resolve `IBridgeToolCatalog`
5. resolve `IBridgeToolExecutor`
6. optionally resolve `ISecurityRedactor`, `IAuditSink`, and `IToolExecutionPolicy` to confirm the security seams are present
7. create a `BridgeToolRequest` for `RegexTextSearchTool.ToolId`
8. execute through `IBridgeToolExecutor.ExecuteAsync`
9. print catalog descriptors, logger entries, audit envelope data when captured, result metadata, and returned matches

For security-aware runs, override the default `NoOpAuditSink` with `InMemoryAuditSink` before calling `AddBridgeToolServices()`, and use a trace-only policy wrapper when you need to print the observed policy evaluation without changing production defaults.

### 2. Use deterministic request input

Baseline input:

```text
ToolId: bridge.regexTextSearch
RequestId: tool-trace-20260509-req-001
OperationId: tool-regex-search-20260509-op-001
pattern: error|warning
caseSensitive: false
maxResults: 10
entries:
- Info: startup complete
- Warning: configuration fallback used
- Error: sample failure marker
- Trace: execution complete
```

Expected result:

- `Success=True`
- `matchCount=2`
- `totalMatchCount=2`
- `limited=False`
- returned values include `Warning` and `Error`

### 3. Capture correlated logs

The executor boundary must produce at least:

```text
Bridge tool execution started [ToolId=bridge.regexTextSearch] [RequestId=tool-trace-20260509-req-001] [OperationId=tool-regex-search-20260509-op-001].
Bridge tool execution completed [ToolId=bridge.regexTextSearch] [RequestId=tool-trace-20260509-req-001] [OperationId=tool-regex-search-20260509-op-001] [Success=True] [ElapsedMs=<n>].
```

Every line in the observed execution boundary should preserve the same request id and operation id.
Trace-level request/result payload logs, when captured, must be redacted before being stored as durable artifacts.

For failure-path traces, capture:

- `ErrorCode`
- failure message
- exception type if one was logged
- whether the failure was structured by the tool or caught by `BridgeToolExecutor`
- whether policy allowed or denied execution
- whether an audit envelope was emitted with request id and operation id

### 4. Preserve durable artifacts

For each new run, create dated files instead of overwriting existing artifacts:

- `artifacts/logs/<run-name>.log`
- `artifacts/logs/<run-name>.metadata.json`
- `docs/diagrams/<run-name>.mmd`
- optionally `docs/session-handoffs/<date>-<topic>.md` when the run changes the resume point

Metadata should include:

- branch
- commit
- tool id
- request id
- operation id
- input summary
- observed result summary
- capture method
- explicit scope exclusions

If `.gitignore` blocks the files, whitelist the exact durable artifact paths.

## Mermaid Generation Pattern

Build the Mermaid sequence from observed logs and result output, not from the intended design alone.

Use this baseline shape for the compiled regex text-search path:

```mermaid
sequenceDiagram
    participant Caller as Trace Harness / Caller
    participant DI as Shared DI Composition
    participant Executor as BridgeToolExecutor
    participant Policy as IToolExecutionPolicy
    participant Catalog as CompiledBridgeToolCatalog
    participant Tool as RegexTextSearchTool
    participant Audit as IAuditSink
    participant Result as BridgeToolResult
    participant Log as RecordingBridgeLogger

    Caller->>DI: AddBridgeToolServices()
    DI-->>Caller: IBridgeToolCatalog + IBridgeToolExecutor
    Caller->>Catalog: GetTools()
    Catalog-->>Caller: bridge.regexTextSearch descriptor
    Caller->>Executor: ExecuteAsync(requestId, operationId, toolId)
    Executor->>Log: Information "Bridge tool execution started"
    Executor->>Log: Trace redacted request payload when Trace is enabled
    Executor->>Catalog: TryGetTool("bridge.regexTextSearch")
    Catalog-->>Executor: RegexTextSearchTool
    Executor->>Policy: EvaluateAsync(ToolExecutionSecurityContext)
    Policy-->>Executor: Allow
    Executor->>Tool: ExecuteAsync(BridgeToolRequest)
    Tool->>Tool: Compile regex "error|warning"
    Tool->>Tool: Search supplied entries
    Tool-->>Executor: BridgeToolResult Success, 2 matches
    Executor->>Log: Information "Bridge tool execution completed"
    Executor->>Log: Trace redacted result payload when Trace is enabled
    Executor->>Audit: RecordAsync(BridgeAuditEnvelope)
    Executor-->>Caller: BridgeToolResult with preserved requestId and operationId
    Caller->>Result: Inspect matchCount, totalMatchCount, limited, matches
```

## Code Comparison Checklist

After generating the sequence, compare it to current code.

Confirm:

- `AddBridgeToolServices` registers `RegexTextSearchTool` as compiled `IBridgeTool`
- `AddBridgeToolServices` registers default security seams through `AddBridgeSecurityServices`
- `CompiledBridgeToolCatalog.GetTools()` exposes the descriptor
- `BridgeToolExecutor.ExecuteAsync` logs start before catalog lookup
- `BridgeToolExecutor.ExecuteAsync` evaluates `IToolExecutionPolicy` before invoking the tool
- `BridgeToolExecutor.ExecuteAsync` emits a `BridgeAuditEnvelope` after terminal outcomes
- payload-oriented executor logs pass through `ISecurityRedactor`
- `BridgeToolExecutor.ExecuteAsync` preserves request id and operation id in all returned results
- `RegexTextSearchTool.ExecuteAsync` returns structured failure for invalid regex
- no MEF, plugin directory, BM25, MCP transport, presenter, or proposal code is involved in this workflow

## Reuse Guidance For Future Sessions

When repeating this workflow:

1. use deterministic request and operation IDs
2. capture the catalog descriptor before execution
3. capture executor boundary logs, audit data when enabled, and final result data
4. confirm no raw secret-like values were written into logs, traces, prompts, exception dumps, or artifacts
5. generate the Mermaid diagram from observed output
6. compare the diagram against code before expanding the tool system
7. keep future MEF/plugin/BM25 traces separate from this compiled-tool baseline
