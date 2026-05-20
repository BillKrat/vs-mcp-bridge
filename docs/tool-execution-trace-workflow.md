# Bridge Tool Execution Trace Workflow

Use this workflow to repeat the observed bridge tool execution validation, capture durable artifacts, and compare the resulting sequence against the current shared tool code.

## Purpose

Provide a repeatable AI-friendly and developer-friendly process for:

- invoking a compiled bridge tool through `IBridgeToolExecutor`
- proving catalog discovery through `CompiledBridgeToolCatalog` and the default compiled discovery adapter
- proving bridge tool manifest metadata derived from descriptors
- proving read-only catalog inventory metadata without executing tools
- proving the minimal policy/capability/approval/secret-reference/redaction/audit seams around execution
- collecting correlated execution-boundary logs
- preserving request and operation correlation IDs
- generating a Mermaid sequence diagram from observed behavior
- producing durable artifacts future sessions can use for triage before expanding into plugin loading or search ranking

## Scope

This workflow documents the shared compiled bridge tool path only.
The catalog may now be fed by discovery providers, but this baseline keeps MEF directory discovery disabled and validates the default compiled tool path.
Tool manifests are lightweight metadata derived from `BridgeToolDescriptor`; this workflow does not introduce persistent manifests, package publishing, remote tools, signed plugin metadata, OAuth/RBAC/user identity, or a MEF redesign.

It does not validate:

- MEF directory discovery
- directory-loaded plugins as a production plugin model
- BM25 trace artifact generation; BM25 remains a compiled in-memory bridge tool covered by unit tests
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
- approval service: `AllowToolExecutionApprovalService`, not invoked for descriptors with `ApprovalRequirement=NotRequired`
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
- approval service: default approval service; `RegexTextSearchTool` does not require approval
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

## Observed MEF Discovery Boundary Run

After the minimal MEF discovery seam was added, the discovery boundary was validated separately from the compiled execution baseline:

- run name: `mef-discovery-trace-20260516`
- branch: `main`
- commit: `8777929`
- catalog: `CompiledBridgeToolCatalog`
- discovery providers: `CompiledBridgeToolDiscovery`, `MefBridgeToolDiscovery`
- executor: `BridgeToolExecutor`
- audit sink: `InMemoryAuditSink`
- MEF test tool id: `fake.mef`
- request id: `mef-discovery-trace-20260516-req-001`
- operation id: `mef-discovery-trace-20260516-op-001`

MEF discovery reference artifacts:

- sequence diagram: [`docs/diagrams/mef-discovery-trace-20260516.mmd`](diagrams/mef-discovery-trace-20260516.mmd)
- observed log transcript: [`artifacts/logs/mef-discovery-trace-20260516.log`](../artifacts/logs/mef-discovery-trace-20260516.log)
- run metadata: [`artifacts/logs/mef-discovery-trace-20260516.metadata.json`](../artifacts/logs/mef-discovery-trace-20260516.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-16-mef-discovery-trace-validation.md`](session-handoffs/2026-05-16-mef-discovery-trace-validation.md)

This trace covers MEF discovery start, configured directories, missing-directory behavior, invalid assembly load behavior, discovery completion, catalog composition, and the preserved executor boundary. It deliberately uses the existing shared-test `MefFakeBridgeTool` export and does not add a production tool.

## Observed Approval-Aware Boundary Run

After approval-aware execution was added to `BridgeToolExecutor`, the approval boundary was documented with the existing shared-test fake approval-required tool path:

- run name: `tool-approval-trace-20260516`
- branch: `main`
- commit: `1d2cfc7`
- catalog: `CompiledBridgeToolCatalog`
- executor: `BridgeToolExecutor`
- policy: `AllowToolExecutionPolicy`
- approval service: shared-test `RecordingToolExecutionApprovalService`
- redactor: `BridgeSecurityRedactor`
- audit sink: `InMemoryAuditSink`
- test tool id: `fake.approvalRequired`
- approved request id: `tool-approval-trace-20260516-allow-req-001`
- denied request id: `tool-approval-trace-20260516-deny-req-001`

Approval-aware reference artifacts:

- sequence diagram: [`docs/diagrams/tool-approval-trace-20260516.mmd`](diagrams/tool-approval-trace-20260516.mmd)
- observed log transcript: [`artifacts/logs/tool-approval-trace-20260516.log`](../artifacts/logs/tool-approval-trace-20260516.log)
- run metadata: [`artifacts/logs/tool-approval-trace-20260516.metadata.json`](../artifacts/logs/tool-approval-trace-20260516.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-16-tool-approval-validation.md`](session-handoffs/2026-05-16-tool-approval-validation.md)

This trace covers approved and denied approval decisions without adding a runtime user-facing tool, UI approval prompt, proposal approval redesign, or MCP transport change.
It proves approval decisions are visible through executor logs, structured results, audit metadata, redaction, and request/operation correlation.

## Observed Manifest Metadata Run

After the lightweight manifest model was added to `BridgeToolDescriptor`, manifest metadata flow was validated with the compiled regex text-search path plus a harness-only approval-required probe:

- run name: `tool-manifest-trace-20260516`
- branch: `main`
- commit: `5e9b71f`
- capture date: `2026-05-20`
- catalog: `CompiledBridgeToolCatalog`
- executor: `BridgeToolExecutor`
- policy: trace-only `RecordingPolicy`
- approval service: default approval service for `RegexTextSearchTool`; harness-only `RecordingApprovalService` for approval-context observation
- redactor: `BridgeSecurityRedactor`
- audit sink: `InMemoryAuditSink`
- compiled tool id: `bridge.regexTextSearch`
- request id: `tool-manifest-trace-20260516-req-001`
- operation id: `tool-manifest-trace-20260516-op-001`
- observed result: success, 2 returned matches, 2 total matches

Manifest metadata reference artifacts:

- sequence diagram: [`docs/diagrams/tool-manifest-trace-20260516.mmd`](diagrams/tool-manifest-trace-20260516.mmd)
- observed log transcript: [`artifacts/logs/tool-manifest-trace-20260516.log`](../artifacts/logs/tool-manifest-trace-20260516.log)
- run metadata: [`artifacts/logs/tool-manifest-trace-20260516.metadata.json`](../artifacts/logs/tool-manifest-trace-20260516.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-16-tool-manifest-validation.md`](session-handoffs/2026-05-16-tool-manifest-validation.md)

This trace proves descriptor-derived manifest metadata is visible through catalog descriptors, `ToolExecutionSecurityContext`, trace logging, and audit metadata.
It also uses a temporary harness-only approval-required tool to observe `ToolExecutionApprovalContext.Manifest` without adding a production tool or changing runtime behavior.
The MEF path is documented from existing shared-test evidence for `MefFakeBridgeTool`; this trace does not change MEF behavior.

## Observed Inventory Snapshot Run

After the read-only catalog inventory seam was added, inventory behavior was validated with compiled tools and an explicitly enabled MEF test-export path:

- run name: `tool-inventory-trace-20260516`
- branch: `main`
- commit: `9708609`
- capture date: `2026-05-20`
- inventory service: `IBridgeToolInventoryService`
- catalog: `CompiledBridgeToolCatalog`
- compiled discovery: `CompiledBridgeToolDiscovery`
- MEF discovery: `MefBridgeToolDiscovery` scanning the existing shared-test assembly
- compiled snapshot order: `bridge.bm25TextSearch`, `bridge.regexTextSearch`
- MEF-enabled snapshot order: `bridge.bm25TextSearch`, `bridge.regexTextSearch`, `fake.mef`
- observed result: deterministic read-only snapshots with no tool execution

Inventory reference artifacts:

- sequence diagram: [`docs/diagrams/tool-inventory-trace-20260516.mmd`](diagrams/tool-inventory-trace-20260516.mmd)
- observed log transcript: [`artifacts/logs/tool-inventory-trace-20260516.log`](../artifacts/logs/tool-inventory-trace-20260516.log)
- run metadata: [`artifacts/logs/tool-inventory-trace-20260516.metadata.json`](../artifacts/logs/tool-inventory-trace-20260516.metadata.json)
- session handoff: [`docs/session-handoffs/2026-05-16-tool-inventory-validation.md`](session-handoffs/2026-05-16-tool-inventory-validation.md)

This trace proves inventory snapshots read descriptor-derived manifest metadata from the catalog and sort by tool id.
At the time of that trace, inventory was not exposed over MCP; the later MCP diagnostic below adds read-only transport visibility without changing bridge tool execution behavior.

## Observed MCP Inventory Diagnostic Run

The bridge tool catalog inventory is now visible through MCP as the diagnostic tool `bridge_get_tool_inventory`:

- run name: `mcp-tool-inventory-trace-20260516`
- branch: `main`
- baseline commit: `644b17e`
- capture date: `2026-05-20`
- MCP tool: `bridge_get_tool_inventory`
- inventory service: `IBridgeToolInventoryService`
- compiled snapshot order: `bridge.bm25TextSearch`, `bridge.regexTextSearch`
- observed result: metadata-only deterministic inventory returned through MCP without tool execution

MCP inventory diagnostic artifacts:

- sequence diagram: [`docs/diagrams/mcp-tool-inventory-trace-20260516.mmd`](diagrams/mcp-tool-inventory-trace-20260516.mmd)
- observed log transcript: [`artifacts/logs/mcp-tool-inventory-trace-20260516.log`](../artifacts/logs/mcp-tool-inventory-trace-20260516.log)
- session handoff: [`docs/session-handoffs/2026-05-16-mcp-tool-inventory-validation.md`](session-handoffs/2026-05-16-mcp-tool-inventory-validation.md)

This diagnostic calls only `IBridgeToolInventoryService.GetSnapshot()`.
It does not invoke `BridgeToolExecutor`, `IToolExecutionPolicy`, `IToolExecutionApprovalService`, `IAuditSink`, `IPipeClient`, `IChatEngine`, or bridge tool `ExecuteAsync`.
It logs request id, elapsed time, and tool count without logging raw payloads or secrets.

## Preconditions

- repository root: `Y:\vs-mcp-bridge`
- branch and commit should be recorded before the run
- current shared tests should pass:

```powershell
dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj
```

- tool execution should use `AddBridgeToolServices()` so the catalog/executor path matches shared composition
- leave `BridgeToolDiscoveryOptions.EnableMefDirectoryDiscovery` disabled for this compiled-tool baseline unless creating a separate MEF discovery trace
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
3. call `services.AddBridgeToolServices()` without enabling MEF directory discovery
4. resolve `IBridgeToolCatalog`
5. resolve `IBridgeToolExecutor`
6. optionally resolve `ISecurityRedactor`, `IAuditSink`, `IToolExecutionPolicy`, and `IToolExecutionApprovalService` to confirm the security seams are present
7. create a `BridgeToolRequest` for `RegexTextSearchTool.ToolId`
8. execute through `IBridgeToolExecutor.ExecuteAsync`
9. print catalog descriptors, derived manifest metadata, logger entries, audit envelope data when captured, result metadata, and returned matches

For security-aware runs, override the default `NoOpAuditSink` with `InMemoryAuditSink` before calling `AddBridgeToolServices()`, and use trace-only policy or approval wrappers when you need to print observed decisions without changing production defaults.
Capability-aware traces should use a fake or test tool descriptor with `RequiredCapabilities` populated; existing compiled tools declare no required capabilities by default.
Capability metadata is policy input only in the current bridge. `CapabilityToolExecutionPolicy` can be used explicitly in tests or harnesses to evaluate static allowed, denied, and unknown required capabilities.
It is not authentication, OAuth scope enforcement, role/user identity, UI permission prompting, persistent policy storage, remote authorization, sandboxing, or production authorization.
Secret-reference traces should use synthetic `SecretReference` values and a fake broker. The default `NoOpSecretBroker` returns unresolved, and unresolved references must produce a structured `SecretReferenceUnresolved` result before tool execution.
Secret references are future-proof indirection metadata only; they are not real secret storage, encryption, Azure Key Vault integration, external provider integration, authentication, persistence, or raw secret injection.
Approval-aware traces should use a fake or test tool descriptor with `ApprovalRequirement=Required`; existing compiled tools remain approval-not-required by default.
The current durable approval trace uses the shared-test `ApprovalRequiredBridgeTool` fixture and `RecordingToolExecutionApprovalService`.

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
- manifest identity, version, category, source/discovery kind, and host affinity
- whether the failure was structured by the tool or caught by `BridgeToolExecutor`
- which required capabilities were declared by the descriptor
- which secret references were declared in the request arguments
- whether secret references resolved or failed structurally
- whether policy allowed or denied execution
- whether tool approval was not required, approved, or denied
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
    participant Approval as IToolExecutionApprovalService
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
    Executor->>Executor: Approval not required by descriptor
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
- `AddBridgeToolServices` registers `Bm25TextSearchTool` as a compiled in-memory `IBridgeTool`
- `AddBridgeToolServices` registers `CompiledBridgeToolDiscovery` and optional `MefBridgeToolDiscovery`
- `AddBridgeToolServices` registers default security seams through `AddBridgeSecurityServices`
- `AddBridgeSecurityServices` registers `IToolExecutionApprovalService`
- `CompiledBridgeToolCatalog.GetTools()` exposes the descriptor
- each descriptor derives a `BridgeToolManifest` with stable id, name, version, description, category, source/discovery kind, required capabilities, approval requirement, risk hint, and optional host affinity
- `IBridgeToolInventoryService.GetSnapshot()` exposes deterministic descriptor-derived manifest inventory metadata ordered by tool id without invoking tools
- `BridgeToolExecutor.ExecuteAsync` logs start before catalog lookup
- `BridgeToolExecutor.ExecuteAsync` logs redacted manifest metadata after catalog lookup
- `BridgeToolExecutor.ExecuteAsync` logs redacted required-capability metadata after catalog lookup
- `ToolExecutionSecurityContext` and `ToolExecutionApprovalContext` expose derived manifest metadata without changing policy or approval defaults
- `BridgeToolExecutor.ExecuteAsync` evaluates `IToolExecutionPolicy` before invoking the tool
- `ToolExecutionSecurityContext.RequiredCapabilities` exposes descriptor-declared capabilities to policy
- `ToolExecutionSecurityContext.SecretReferences` exposes structured request secret references to policy
- `CapabilityToolExecutionPolicy` is optional and is not the default DI policy
- when `CapabilityToolExecutionPolicy` denies, `BridgeToolExecutor` returns a structured `PolicyDenied` result before approval or tool execution
- `BridgeToolExecutor.ExecuteAsync` evaluates approval only when the descriptor requires it
- approval-denied executions return structured `ApprovalDenied` failures and do not invoke the tool
- unresolved secret references return structured `SecretReferenceUnresolved` failures and do not invoke the tool
- `BridgeToolExecutor.ExecuteAsync` emits a `BridgeAuditEnvelope` after terminal outcomes
- audit metadata includes manifest identity/version/category/source/discovery/host metadata, redacted required capabilities, secret references, secret resolution status, approval requirement, approval decision, and redacted approval reason
- audit classification metadata includes category, severity, risk level, and outcome for success, policy denial, approval denial, unresolved secret references, cancellation, and execution failure
- payload-oriented executor logs pass through `ISecurityRedactor`
- `BridgeToolExecutor.ExecuteAsync` preserves request id and operation id in all returned results
- `RegexTextSearchTool.ExecuteAsync` returns structured failure for invalid regex
- `Bm25TextSearchTool.ExecuteAsync` remains request-scoped and in-memory with no persistent index or crawler
- MEF directory discovery is not enabled, and no plugin directory, MCP transport, presenter, or proposal code is involved in this workflow

## MEF Discovery Boundary Note

MEF is discovery only. It may add exported `IBridgeTool` instances to the shared catalog when explicitly configured, but it does not execute tools during discovery and does not replace `BridgeToolExecutor`.
All discovered tools must still flow through executor policy evaluation, approval evaluation when required, redacted payload logging, terminal audit envelope emission, and request/operation correlation preservation.
Directory-loaded tools are not production sandboxing; plugin/tool authors do not own core audit, redaction, policy, approval, or correlation behavior.

When validating MEF discovery, keep the artifact separate from the compiled execution baseline and capture these boundaries:

- discovery start with `EnableMefDirectoryDiscovery`, directory count, and search pattern
- each configured directory outcome, including missing directories
- assembly-load warnings for invalid candidate DLLs
- discovery completion with assembly count and tool count
- composed catalog descriptors showing compiled and MEF sources
- proof that MEF-discovered tools remain unexecuted during discovery
- one executor call proving `BridgeToolExecutor` still owns policy, redaction, audit, and correlation

## Reuse Guidance For Future Sessions

When repeating this workflow:

1. use deterministic request and operation IDs
2. capture the catalog descriptor before execution
3. capture executor boundary logs, audit data when enabled, and final result data
4. confirm no raw secret-like values were written into logs, traces, prompts, exception dumps, or artifacts
5. include approval metadata when validating an approval-required test tool
6. generate the Mermaid diagram from observed output
7. compare the diagram against code before expanding the tool system
8. keep future MEF/plugin/BM25 traces separate from this compiled-tool baseline
