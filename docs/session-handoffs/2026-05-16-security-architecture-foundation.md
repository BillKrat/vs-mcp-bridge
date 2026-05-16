# Foundational Security Architecture Handoff

## Summary

The MCP bridge now has contract-first security seams around bridge tool execution without changing default runtime behavior.

Current boundary:

- branch: `main`
- baseline commit: `7cd6892 Add structured audit classification metadata`
- primary boundary: `BridgeToolExecutor`
- default behavior: existing tools run automatically unless a host or test explicitly configures policy, approval, or secret broker behavior

This handoff is a scope guard for future sessions. It documents what exists, what it protects, and what remains intentionally deferred.

## Implemented Seams

`BridgeToolExecutor` is the single execution/security boundary for bridge tools. Callers, discovered tools, plugins, and future host surfaces must not execute bridge tools directly or bypass this executor.

The current executor-owned sequence is:

1. start logging with request id and operation id
2. discover the tool from `IBridgeToolCatalog`
3. build `ToolExecutionSecurityContext`
4. evaluate `IToolExecutionPolicy`
5. evaluate `IToolExecutionApprovalService` only when the descriptor requires approval
6. resolve structured `ISecretReference` values through `ISecretBroker`
7. execute the tool only after policy, approval, and secret-reference checks pass
8. redact payload-oriented logs and audit metadata through `ISecurityRedactor`
9. emit a terminal `BridgeAuditEnvelope`
10. preserve request id and operation id in results, logs, policy/approval context, and audit envelopes

Implemented contracts and behavior:

- `IToolExecutionPolicy` can allow or deny execution before the tool runs.
- `BridgeToolDescriptor.RequiredCapabilities` exposes declarative `BridgeCapability` metadata to policy and audit.
- `CapabilityToolExecutionPolicy` is an optional static capability policy for allowed, denied, and unknown capability names.
- `ToolExecutionApprovalRequirement` lets selected descriptors require approval.
- `IToolExecutionApprovalService` can approve or deny required-approval executions.
- approval denial returns structured `ApprovalDenied` and does not invoke the tool.
- `ISecretReference`, `SecretReference`, and `SecretReferenceKind` let requests point at secrets indirectly.
- `ISecretBroker` is the secret-resolution seam; `NoOpSecretBroker` fails safely with unresolved status.
- unresolved secret references return structured `SecretReferenceUnresolved` before tool execution.
- `ISecurityRedactor` masks obvious secret-like values in logs and audit metadata.
- `BridgeAuditEnvelope` captures terminal execution metadata, policy/approval/secret metadata, required capabilities, correlation ids, and outcome state.
- audit classification fields categorize terminal audit events by category, severity, risk level, and outcome.
- MEF discovery remains discovery only; discovered tools still execute through `BridgeToolExecutor`.

## What These Seams Protect

The architecture currently protects these boundaries:

- tools cannot be considered policy-checked unless they ran through `BridgeToolExecutor`
- selected tools can be denied by policy before approval, secret resolution, or execution
- selected tools can require an approval decision before execution
- structured secret references can be observed without placing raw secret values in normal request payloads
- denial and failure outcomes return structured results rather than relying on chat history or implicit exceptions
- request and operation correlation is reconstructable across logs, audit envelopes, and results
- audit events can be categorized consistently without changing where audits are stored

## Operational Rules

- Tools and plugins must not bypass `BridgeToolExecutor`.
- Tools and plugins must not log raw secrets, tokens, passwords, bearer values, or unredacted exception payloads.
- Secret values should flow by structured reference, not by raw payload, when a tool needs future secret access.
- Capability metadata is declarative plumbing for policy decisions, not a full authorization system.
- Approval-aware execution is a tool policy seam, not a UI prompt and not the proposal approval workflow.
- Audit classification is observability metadata, not a compliance framework, SIEM integration, or audit store.
- MEF composition must stay discovery-only; it must not become an execution bypass or plugin sandbox.

## Intentionally Deferred

Do not expand scope prematurely into:

- OAuth/authentication
- user identity, roles, or RBAC
- real secret storage or vault integration
- encrypted persistence
- remote authorization
- plugin sandboxing
- signed plugin manifests
- tamper-evident audit storage
- SIEM/export pipelines
- compliance framework mappings
- UI approval prompts
- remote execution

## Resume Guidance

For future security slices, start from `docs/ARCHITECTURE.md`, this handoff, and the durable trace workflow in `docs/tool-execution-trace-workflow.md`.

When adding a new tool or discovery path, verify:

- descriptor metadata is visible before execution
- policy can inspect required capabilities and secret references
- approval is evaluated only when the descriptor requires it
- unresolved secret references fail before tool invocation
- audit envelopes preserve correlation and classification metadata
- redaction is applied before logs or audit metadata leave the executor boundary

When adding real auth, vaults, UI approval prompts, plugin isolation, or audit export later, keep those changes behind explicit contracts and preserve the current executor boundary.
