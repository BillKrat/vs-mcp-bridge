# Tool Approval Trace Validation Handoff

## Summary

Approval-aware bridge tool execution now has durable trace evidence for both terminal approval outcomes.

Observed trace:

- run name: `tool-approval-trace-20260516`
- branch: `main`
- observed commit: `1d2cfc7`
- tool path: `CompiledBridgeToolCatalog` -> `BridgeToolExecutor` -> shared-test `ApprovalRequiredBridgeTool`
- policy: `AllowToolExecutionPolicy`
- approval service: shared-test `RecordingToolExecutionApprovalService`
- redactor: `BridgeSecurityRedactor`
- audit sink: `InMemoryAuditSink`
- approved request id: `tool-approval-trace-20260516-allow-req-001`
- approved operation id: `tool-approval-trace-20260516-allow-op-001`
- denied request id: `tool-approval-trace-20260516-deny-req-001`
- denied operation id: `tool-approval-trace-20260516-deny-op-001`

Durable artifacts:

- `SolutionFolder/artifacts/logs/tool-approval-trace-20260516.log`
- `SolutionFolder/artifacts/logs/tool-approval-trace-20260516.metadata.json`
- `SolutionFolder/docs/diagrams/tool-approval-trace-20260516.mmd`

## What Was Proven

- `BridgeToolExecutor` remains the policy, approval, execution, audit, redaction, and correlation boundary.
- `IToolExecutionPolicy` is evaluated before approval and before tool execution.
- descriptor metadata with `ApprovalRequirement=Required` causes the executor to call `IToolExecutionApprovalService`.
- an approved decision allows the tool to execute and emits a successful audit envelope.
- a denied decision prevents tool execution and returns a structured `ApprovalDenied` result.
- both terminal outcomes emit `BridgeAuditEnvelope` records with approval requirement, decision, and redacted approval reason metadata.
- request id and operation id are preserved across policy evaluation, approval evaluation, executor logs, audit emission, and result metadata.
- durable artifacts contain redacted approval reason evidence only.

## Scope Exclusions

This was not a runtime tool addition, UI approval prompt, proposal approval redesign, MCP transport validation, VSIX host validation, App host validation, OAuth/authentication, sandboxing, remote execution, or MEF production plugin validation.

The fake approval-required tool is a shared-test fixture used to prove the executor seam. It is not a user-facing bridge tool.

## Resume Guidance

Use `SolutionFolder/docs/tool-execution-trace-workflow.md` for repeatable trace capture and `SolutionFolder/docs/diagrams/tool-approval-trace-20260516.mmd` for the reconstructed sequence.

Before adding runtime approval prompts or marking production tools as approval-required, preserve these invariants:

- keep approval inside `BridgeToolExecutor`
- keep policy evaluation before approval
- keep approval denial as a structured result, not an exception path
- keep approval metadata in the audit envelope
- keep approval reason metadata redacted
- keep proposal approval workflow separate from tool execution approval
