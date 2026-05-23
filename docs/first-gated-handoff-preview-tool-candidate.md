# First Gated Handoff Preview Tool Candidate

## Purpose

Identify the single highest-leverage first bridge-side preview/proposal capability that fits the current operational doctrine.

This is a design candidate only. It does not add runtime code, tool implementation, mutation behavior, Codex execution, background workflow, deployment behavior, or approval changes.

## Problem Statement

The current manual ChatGPT to Codex handoff works well because the human remains in control:

- ChatGPT helps scope the next slice.
- The user approves the slice.
- Codex executes the approved repository work.
- Codex reports files, validation, commit, blockers, and final status.
- The user decides whether to ask questions, proceed, or stop.

The remaining friction is repetitive copy/paste and formatting effort. Future tooling should reduce that friction without removing human approval gates or creating hidden execution paths.

## Proposed First Capability

The first bridge-side capability should generate a structured handoff preview only.

It should not:

- execute Codex
- mutate the repository
- invoke Codex
- start a background workflow
- continue automatically
- deploy
- run shell commands
- perform git operations

Suggested capability:

- `codex.gatedHandoffPreview`

Suggested tool names:

- compiled bridge tool: `bridge.codexGatedHandoffPreview`
- MCP wrapper: `bridge_codex_gated_handoff_preview`

## Input Shape

The preview should accept explicit caller-provided input:

- slice objective
- repo path
- constraints and non-goals
- validation requirements
- expected artifacts
- deployment restrictions
- risk flags
- optional allowed files or areas
- optional request id
- optional operation id

Inputs should be treated as proposal material, not execution permission.

## Output Shape

The preview should return a structured handoff proposal:

- correlation id
- request id
- operation id
- summary
- scoped task text
- repo target
- constraints and non-goals
- validation checklist
- risk indicators
- approval indicators
- artifact expectations
- deployment restrictions
- stop conditions
- redaction status
- terminal preview status

Suggested status values:

- `PreviewGenerated`
- `InvalidRequest`
- `MissingScope`
- `MissingRepoTarget`
- `MissingValidationPlan`
- `UnsafeExpansion`
- `SecretRedacted`
- `PreviewFailed`

The result should be compact enough for ChatGPT to summarize directly to the user.

## Why This Is The Correct First Tool

This is the right first capability because it reduces handoff friction without changing the control model.

Benefits:

- reduces repetitive copy/paste
- preserves the human approval gate
- preserves narrow-slice discipline
- preserves auditability and correlation
- keeps operational risk low
- fits the preview-only mutation philosophy
- fits existing bridge manifest, capability, redaction, audit, and correlation seams
- creates a reusable contract before any execution-capable workflow exists

The first useful proof is not whether the bridge can run Codex. The first useful proof is whether the bridge can produce a clear, redacted, auditable handoff proposal that a human can approve or reject.

## Acceptable Use Examples

Acceptable preview requests:

- "Create a docs-only handoff for the latest BlogAI deployment state; validation is `git diff --check`; no runtime code; no deploy."
- "Prepare a Codex task to update existing evidence docs with a successful smoke result; do not execute."
- "Normalize a future implementation slice for a preview-only bridge tool; expected validation is shared tests plus `git diff --check`."
- "Create a deployment-readiness handoff preview that lists pre-checks and smoke URLs without publishing."
- "Prepare a task summary for adding `.gitignore` allow-list entries for explicitly named docs only."

These requests are acceptable because they produce a preview/proposal and leave execution as a separate user-approved step.

## Unsafe Expansion Examples

Unsafe expansions the tool should reject or flag:

- "Generate the handoff and run Codex automatically."
- "If validation passes, commit, push, and start the next slice."
- "Publish BlogAI after preparing the deployment task."
- "Find every related file and update them."
- "Create the preview, then apply the changes."
- "Use the deployment password directly in the task text."
- "Keep polling Codex until it finishes and continue with the next task."
- "Reset the branch if the working tree is not clean."
- "Convert `/local-dev` diagnostics into production auth as part of the handoff."

These requests cross from preview/proposal into execution, mutation, deployment, destructive git behavior, secret exposure, or scope expansion.

## Explicit Rejections

The first tool must explicitly reject or avoid:

- autonomous Codex execution
- automatic mutation
- hidden repo edits
- background loops
- auto-approval
- automatic deployment
- speculative orchestration
- destructive git operations
- broad repository crawling
- hidden file selection
- production-auth implementation
- persistence or middleware changes
- raw secret capture

## Approval Boundary

The preview result is not approval.

Before any future execution-capable workflow sends a task to Codex, a user-visible approval decision must confirm:

- exact task text
- repo target
- constraints and non-goals
- validation expectations
- allowed files or areas, if any
- artifact expectations
- commit/push expectation
- risk flags
- request id and operation id

Approval must not be inferred from the existence of a generated preview.

## Audit And Redaction Expectations

The future implementation should route through `BridgeToolExecutor` and preserve:

- manifest metadata
- capability metadata
- policy evaluation
- redaction before logs and audit metadata
- audit envelope emission
- request and operation correlation
- structured success/failure results

Audit metadata may include:

- repo target
- task summary
- validation checklist summary
- risk flags
- approval-required flag
- redaction status
- terminal status

Audit metadata must not include:

- raw passwords
- raw environment variable values
- tokens
- cookies
- bearer values
- full unredacted prompts
- full file bodies
- unredacted terminal output

## Future Readiness Conditions Before Execution Tooling

Execution-capable tooling should not exist until these conditions are met:

- preview contract is implemented and validated
- stop conditions are proven with tests
- redaction behavior is tested against secret-like inputs
- correlation ids flow through preview results and audit envelopes
- human approval boundary is explicit
- task result contract is designed
- Codex execution lifecycle has timeout, cancellation, and terminal status design
- no auto-continuation is the default
- destructive git and deployment actions require separate explicit approval
- durable evidence format is defined for meaningful executions

## Smallest Future Implementation Slice

Implement only a no-side-effect preview tool:

- validate explicit input
- normalize scoped task text
- generate request and operation ids when absent
- redact secret-like values
- return structured handoff proposal
- emit audit metadata
- add tests for valid preview, invalid request, unsafe expansion, redaction, and correlation

Do not invoke Codex, mutate files, run shell commands, deploy, commit, push, or start any background workflow in the first implementation slice.
