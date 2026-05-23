# Gated Handoff Preview Tool Implementation Readiness

## Purpose

Review whether `vs-mcp-bridge` is ready to implement the preview-only gated handoff proposal tool.

This is documentation only. It does not add runtime code, tool implementation, repo mutation behavior, Codex execution, deployment behavior, or background workflow.

## Inputs Reviewed

- `AI_START.md`
- `docs/chatgpt-codex-gated-handoff-workflow.md`
- `docs/session-handoffs/2026-05-17-gated-handoff-tool-readiness-review.md`
- `docs/first-gated-handoff-preview-tool-candidate.md`
- `docs/future-contributor-operating-expectations.md`
- `docs/session-slice-operating-template.md`

## Readiness Decision

The repo is ready for a preview-only compiled bridge tool contract and implementation.

It is not ready for Codex execution, background orchestration, repo mutation, deployment automation, command execution, or auto-continuation.

The first implementation is acceptable only if it:

- returns a structured handoff preview only
- performs no repo writes
- does not invoke Codex
- does not deploy
- does not run commands
- does not auto-continue
- preserves correlation and audit metadata
- redacts secret-shaped input
- flags risky, destructive, deployment, and broad-scope requests
- requires user approval outside the tool before any execution happens

## First Contract

The first implementation should expose a preview-only handoff proposal contract.

Suggested names:

- `GatedHandoffPreviewTool`
- `GatedHandoffPreviewRequest`
- `GatedHandoffPreviewResult`

Suggested bridge identity:

- compiled bridge tool id: `bridge.codexGatedHandoffPreview`
- MCP wrapper name, if exposed in the first slice: `bridge_codex_gated_handoff_preview`
- capability metadata: `codex.gatedHandoffPreview`
- audit classification: `CodexHandoffPreview`
- approval requirement: `NotRequired` for preview-only behavior

The result must still state that Codex execution requires separate user approval outside the tool.

## Contract Location

Place the contract with the existing bridge tool contracts and compiled tool patterns.

Expected implementation areas for the future slice:

- `VsMcpBridge.Shared/Tools` for the compiled tool and descriptor
- `VsMcpBridge.Shared/Tools` or a nearby shared contracts namespace for request/result models, following existing tool model patterns
- `VsMcpBridge.Shared.Tests` for contract, validation, redaction, and correlation tests
- `VsMcpBridge.McpServer` only if the same slice explicitly exposes an MCP wrapper

Do not introduce a new package, external service, background worker, persistent store, or orchestration host for this first implementation.

## Compiled Bridge Tool Decision

Yes, the first tool should be a compiled bridge tool.

Reasons:

- compiled tools are the stable default path
- descriptor metadata can flow through the existing manifest path
- execution routes through `BridgeToolExecutor`
- policy, redaction, audit, and correlation seams are already established
- tests can exercise the same boundary as existing bridge tools
- MEF remains discovery-only and should not be used for this first implementation

## Required Input Model Fields

`GatedHandoffPreviewRequest` should require:

- `SliceObjective`
- `RepoPath`
- `Constraints`
- `NonGoals`
- `ValidationRequirements`

It should also support:

- `ExpectedArtifacts`
- `DeploymentRestrictions`
- `RiskFlags`
- `AllowedFilesOrAreas`
- `CommitPushExpectation`
- `RequestId`
- `OperationId`

Input should be explicit caller-provided text or lists. The tool should not crawl the repository to infer scope.

## Required Output Model Fields

`GatedHandoffPreviewResult` should include:

- `Success`
- `Status`
- `ToolId`
- `RequestId`
- `OperationId`
- `CorrelationId`
- `PreviewOnly`
- `RepoTarget`
- `Summary`
- `ScopedTaskText`
- `Constraints`
- `NonGoals`
- `ValidationChecklist`
- `ExpectedArtifacts`
- `DeploymentRestrictions`
- `RiskFlags`
- `ApprovalRequiredBeforeCodex`
- `StopConditions`
- `RedactionApplied`
- `Message`

Recommended statuses:

- `PreviewGenerated`
- `InvalidRequest`
- `MissingObjective`
- `MissingRepoTarget`
- `MissingValidationPlan`
- `UnsafeExpansion`
- `SecretRedacted`
- `PreviewFailed`

The output should be deterministic for the same input.

## Validation Behavior

The tool should validate:

- objective is present
- repo path is present
- validation requirements are present
- constraints and non-goals are preserved
- deployment restrictions are preserved
- broad or ambiguous scope is flagged
- destructive git terms are flagged
- deployment or publishing terms are flagged
- mutation/apply/write terms are flagged
- auto-continue/background-loop terms are flagged
- secret-shaped input is redacted or marked

Validation should produce structured statuses and risk flags, not exception text dumps.

## Redaction Behavior

The implementation must redact secret-shaped input before logs, audit metadata, and durable summaries.

At minimum, tests should cover secret-shaped keys or values involving:

- password
- token
- secret
- apiKey
- bearer
- authorization
- cookie
- publish profile credentials

The tool should not preserve raw secret values in `ScopedTaskText`, audit metadata, logs, or result messages.

## Audit And Correlation Behavior

The implementation should:

- route through `BridgeToolExecutor`
- preserve caller-supplied `RequestId` and `OperationId`
- generate missing ids when absent
- include manifest identity in execution metadata
- declare capability metadata
- emit audit metadata for terminal preview outcomes
- include `PreviewOnly=true`
- include `ApprovalRequiredBeforeCodex=true`
- include risk flags and redaction status

Audit metadata should not include raw prompts, raw secrets, full file bodies, or terminal output.

## Required Tests

The implementation slice should add focused tests for:

- valid request returns `PreviewGenerated`
- missing objective returns `MissingObjective`
- missing repo path returns `MissingRepoTarget`
- missing validation requirements returns `MissingValidationPlan`
- constraints and non-goals are preserved
- generated scoped task text includes the objective, constraints, validation, and artifact expectations
- request id and operation id are preserved when provided
- request id and operation id are generated when absent
- secret-shaped input is redacted
- destructive git scope is flagged
- deployment/publishing scope is flagged
- mutation/apply/write scope is flagged
- auto-continue/background-loop scope is flagged
- result states `PreviewOnly=true`
- result states `ApprovalRequiredBeforeCodex=true`
- no file writes occur
- no command execution path exists
- no Codex invocation path exists
- execution through `BridgeToolExecutor` preserves audit/correlation metadata

If an MCP wrapper is included, add wrapper tests that prove it routes through `BridgeToolExecutor` and returns the same structured preview without side effects.

## Stop Conditions

The tool must stop before execution when:

- objective is missing
- repo target is missing
- validation plan is missing
- request asks to invoke Codex
- request asks to continue automatically
- request asks for a background loop or polling
- request asks to write, apply, create, delete, rename, or mutate files
- request asks to deploy or publish
- request asks to reset, force push, checkout, or delete branches
- request contains secret-shaped values that cannot be safely redacted
- scope is too broad to preserve a narrow slice

Stop conditions should appear in the result for ChatGPT and the user to review.

## Smallest Next Implementation Slice

The smallest next implementation slice is:

1. Add `GatedHandoffPreviewRequest` and `GatedHandoffPreviewResult`.
2. Add `GatedHandoffPreviewTool` as a compiled bridge tool.
3. Register descriptor metadata, capability metadata, and audit classification.
4. Implement deterministic validation, redaction, risk flags, and scoped task preview generation.
5. Add focused shared tests.
6. Run `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj` and `git diff --check`.

Do not add MCP wrapper exposure unless the implementation slice explicitly includes it and keeps the wrapper preview-only.

Do not invoke Codex, mutate repo files, deploy, run shell commands, commit from inside the tool, push from inside the tool, poll background work, or auto-continue.

## Conclusion

The repo is ready for implementation of a preview-only compiled bridge tool contract named around `GatedHandoffPreview`.

The implementation should produce a structured handoff proposal/result contract and prove validation, redaction, risk flagging, audit metadata, and correlation. Execution remains a separate future design and must require an explicit user approval boundary.
