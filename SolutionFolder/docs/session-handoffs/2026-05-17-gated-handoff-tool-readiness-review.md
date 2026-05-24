# Gated Handoff Tool Readiness Review

## Purpose

Review whether `vs-mcp-bridge` is ready to implement a first bridge-side ChatGPT to Codex gated handoff tool.

This is documentation only. It does not add runtime code, tool implementation, mutation behavior, Codex invocation, background work, deployment behavior, or approval changes.

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/chatgpt-codex-gated-handoff-workflow.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/mcp-controlled-mutation-threshold.md`
- `SolutionFolder/docs/preview-only-document-update-tool-design.md`

## Readiness Decision

The repo is ready only for a preview/proposal tool, not automatic Codex execution.

The current bridge has the right foundational seams for a first handoff-contract tool:

- manifest metadata
- capability metadata
- `BridgeToolExecutor` routing
- policy evaluation
- approval metadata
- redaction
- audit envelopes
- request and operation correlation
- structured success and failure results

Those seams are enough to preview and validate a handoff payload. They are not enough to safely run Codex, wait for background completion, continue automatically, mutate the repository, deploy, or perform destructive git actions.

## First Tool Capability

The first tool should expose one capability: create a structured gated handoff preview from explicit caller input.

Suggested capability name:

- `codex.gatedHandoffPreview`

Suggested tool identity:

- compiled tool id: `bridge.codexGatedHandoffPreview`
- MCP wrapper name: `bridge_codex_gated_handoff_preview`

The tool should accept explicit input:

- scoped task text
- repo path or repo identifier
- expected validation commands
- constraints and non-goals
- commit/push expectation
- optional allowed files or allowed areas
- optional caller-supplied request id and operation id

The tool should return a structured preview only. It should not submit anything to Codex.

## Preview Rather Than Execution

The first tool should be preview/proposal-only.

Reasons:

- the existing design requires no auto-continuation after Codex returns
- the bridge currently has no Codex execution orchestration boundary
- result collection from Codex has not been designed or validated
- background wait loops would create ambiguous ownership and stop conditions
- automatic Codex execution could look like approval when it is only task preparation
- repository mutation, deploy, and destructive git actions must remain outside this first tool

Preview-only behavior keeps the tool below the mutation and automation threshold while proving task shape, risk classification, redaction, and audit metadata.

## Approval Boundary

Codex must not receive a task until after a user-visible approval decision.

The preview tool itself may be non-mutating, but the result must clearly state:

- whether Codex execution would require approval
- what task would be sent
- what repository would be targeted
- what validation would be expected
- whether commit or push is requested
- what constraints and non-goals apply
- what risk flags were detected
- what request id and operation id would carry forward

Approval should not be inferred from:

- the existence of a ChatGPT prompt
- a prior search result
- a prior preview
- clean git state
- branch state
- a successful validation command
- previous consent for a different slice

User approval remains required between slices.

## Result Shape To ChatGPT

The preview result returned to ChatGPT should be structured and compact.

Recommended fields:

```json
{
  "success": true,
  "toolId": "bridge.codexGatedHandoffPreview",
  "requestId": "...",
  "operationId": "...",
  "previewOnly": true,
  "repoTarget": "...",
  "taskSummary": "...",
  "constraints": [],
  "nonGoals": [],
  "validationPlan": [],
  "commitPushExpectation": "none|commit-only|commit-and-push|unspecified",
  "riskFlags": [],
  "approvalRequiredBeforeCodex": true,
  "redactionApplied": true,
  "status": "PreviewGenerated",
  "message": "Handoff preview generated. No Codex task was submitted."
}
```

Failure statuses should also be structured:

- `InvalidRequest`
- `MissingTask`
- `MissingRepoTarget`
- `MissingValidationPlan`
- `UnsafeConstraint`
- `SecretRedacted`
- `PreviewFailed`

The result should be designed so ChatGPT can summarize it without parsing logs or terminal output.

## Correlation, Audit, And Redaction

The implementation should preserve the existing bridge security shape:

- route execution through `BridgeToolExecutor`
- derive manifest metadata from the descriptor
- declare handoff-preview capability metadata
- preserve caller-supplied request id and operation id when provided
- generate missing correlation ids when absent
- redact secret-like values before logs, summaries, audit metadata, and structured results
- emit an audit envelope for terminal preview outcomes
- include audit classification such as `CodexHandoffPreview`
- keep raw full prompts out of durable audit metadata when they may contain sensitive content

Audit metadata should include:

- repo target
- task summary
- validation command names or summaries
- approval-required flag
- risk flags
- redaction status
- terminal status

Audit metadata should not include:

- raw credentials
- raw environment variable values
- bearer tokens
- passwords
- full unredacted prompts
- full file bodies
- unredacted terminal output

## Stop Conditions

The preview tool must stop without Codex invocation when any of these conditions appear:

- missing or empty scoped task
- missing repo target
- broad or ambiguous task scope
- request asks for automatic continuation
- request asks for destructive git behavior without explicit approval
- request asks to deploy, publish, reset, force push, or delete branches
- request asks to mutate files as part of preview
- request asks to hide mutation inside search, inventory, or diagnostics
- request contains raw secret-like values that cannot be safely redacted
- request asks for background wait loops or unattended follow-up
- validation expectations are absent for an implementation slice
- constraints conflict with requested actions

Stop results should be structured and should explain what input must be clarified before another preview is generated.

## Smallest Future Implementation Slice

The smallest future implementation slice is a no-side-effect handoff preview tool.

Implementation scope:

- add a compiled bridge tool that accepts the explicit handoff input
- add descriptor metadata and capability metadata
- route through `BridgeToolExecutor`
- normalize the task into a structured preview
- generate or preserve request id and operation id
- apply existing redaction before logs and audit metadata
- return deterministic statuses and risk flags
- add unit tests for validation, redaction, correlation, and no side effects

Explicitly out of scope:

- Codex invocation
- background job runner
- polling or waiting for Codex
- repository mutation
- file creation or editing
- commit, push, reset, checkout, branch deletion, or force push
- deployment or publishing
- auto-continuation after a result
- UI approval prompts
- persistent audit store
- changes to MCP transport

## Future Result Contract

The eventual execution-capable workflow should return these fields after a Codex run, but that is not part of the first tool:

- summary
- changed files
- validation commands and results
- commit hash if pushed
- blockers or errors
- terminal status
- request id and operation id
- redaction status

The preview tool should reserve these fields or describe the future contract, but it should not fabricate execution results.

## Conclusion

The repo is ready for a first bridge-side gated handoff preview/proposal tool.

It is not ready for automatic Codex execution. The first implementation should create a structured handoff proposal/result contract with no repo mutation, no Codex execution, no background wait loop, and no auto-continuation. User approval remains required between slices.
