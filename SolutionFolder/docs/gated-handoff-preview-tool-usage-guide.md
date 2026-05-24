# Gated Handoff Preview Tool Usage Guide

## Purpose

This guide documents how the preview-only gated handoff tool should be used operationally by ChatGPT, Codex, human operators, and future contributors.

The tool helps produce a structured, redacted, auditable handoff proposal. It does not approve work, execute Codex, run commands, mutate repositories, deploy, start background jobs, or continue automatically.

## Operating Workflow

Use this workflow for ChatGPT to Codex handoffs:

1. Human defines the objective.
2. ChatGPT scopes the slice.
3. Preview tool generates a structured handoff proposal.
4. Human reviews the proposal.
5. Human decides:
   - proceed
   - modify
   - reject
6. Codex receives the approved handoff only.
7. Codex reports results.
8. ChatGPT summarizes the result to the human.
9. Human approves, modifies, or stops before any next slice.

A generated preview is not approval. A successful Codex result is also not approval for the next slice.

## Role Expectations

Human operators:

- define the objective and decision boundary
- approve, modify, or reject the generated proposal
- decide whether the next slice should proceed
- keep deployment, destructive git, and production changes explicit

ChatGPT:

- turns the human objective into a narrow slice
- calls the preview tool with explicit constraints
- summarizes the preview for review
- does not submit follow-up work after Codex returns without a human decision

Codex:

- receives only an approved handoff
- executes one scoped slice
- reports changed files, validation results, commit hash if pushed, and blockers
- stops after reporting

Future contributors:

- preserve preview/proposal before execution
- preserve audit, correlation, and redaction metadata
- keep execution-capable workflows separate from preview-only tooling

## Example Preview Request

```json
{
  "sliceObjective": "Create a docs-only recovery validation handoff for local-only file inventory guidance.",
  "repoPath": "Y:\\vs-mcp-bridge",
  "constraints": [
    "No runtime code",
    "No deployment",
    "No real credential file changes",
    "No publish profile changes"
  ],
  "validationRequirements": [
    "git diff --check",
    "dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj"
  ],
  "expectedArtifacts": [
    "SolutionFolder/docs/session-handoffs/YYYY-MM-DD-local-only-file-recovery-validation.md"
  ],
  "deploymentRestrictions": [
    "Deployment is out of scope"
  ],
  "riskFlags": [
    "SecretHandling",
    "RepoMutation"
  ],
  "requestId": "chatgpt-20260524-local-only-recovery",
  "operationId": "handoff-preview-20260524-local-only-recovery"
}
```

## Example Preview Response

```json
{
  "status": "PreviewGenerated",
  "correlationId": "corr-20260524-local-only-recovery",
  "requestId": "chatgpt-20260524-local-only-recovery",
  "operationId": "handoff-preview-20260524-local-only-recovery",
  "taskSummary": "Create a docs-only recovery validation handoff for local-only file inventory guidance.",
  "scopedTaskText": "Current checkpoint: ... Next smallest slice: Create a docs-only recovery validation handoff ...",
  "constraintsAndNonGoals": [
    "No runtime code",
    "No deployment",
    "No real credential file changes",
    "No publish profile changes"
  ],
  "validationChecklist": [
    "git diff --check",
    "dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj"
  ],
  "expectedArtifacts": [
    "SolutionFolder/docs/session-handoffs/YYYY-MM-DD-local-only-file-recovery-validation.md"
  ],
  "riskFlags": [
    "SecretHandling",
    "RepoMutation"
  ],
  "approvalReminder": "Human approval is required before Codex receives this task.",
  "redactionNotice": "Secret-shaped values were redacted where detected.",
  "sideEffects": {
    "codexExecutionInvoked": false,
    "commandExecutionPerformed": false,
    "repoMutationPerformed": false,
    "deploymentPerformed": false,
    "backgroundWorkflowStarted": false
  }
}
```

The exact serialized result may evolve with the compiled contract, but the operational meaning should not: preview generated, no execution performed, approval still required.

## Risk Flag Interpretation

Risk flags are review signals, not blockers by themselves.

- `Deployment`: publishing, smoke tests, hosting configuration, or deployed URLs appear in scope.
- `DestructiveGit`: reset, checkout, clean, force push, branch deletion, or history rewrite appears in scope.
- `ProductionAuth`: production authentication, OAuth/OpenID/RBAC, middleware, cookies, or sessions appear in scope.
- `SecretHandling`: passwords, tokens, cookies, bearer values, API keys, credential files, or secret-like assignments appear in scope.
- `CommandExecution`: shell commands, scripts, tests, builds, or deployment commands appear in scope.
- `RepoMutation`: file edits, generated artifacts, commits, pushes, deletes, or moves appear in scope.
- `CodexExecution`: the request asks the tool to run Codex, poll Codex, or continue after Codex returns.

If a risk flag is unexpected, modify or reject the preview. If the flag is expected, the human approval should explicitly acknowledge the boundary.

## Approval Expectations

Human approval should confirm:

- exact scoped task text
- repository path
- constraints and non-goals
- validation commands
- expected artifacts
- deployment restrictions
- commit/push expectation
- risk flags
- request id, operation id, or other correlation metadata

Approval for one slice does not authorize the next slice. Approval must not be inferred from a clean working tree, prior success, or a generated preview.

## Correlation And Audit Expectations

Every preview should preserve or generate:

- correlation id
- request id
- operation id
- task summary
- repo target
- risk flags
- validation checklist
- approval reminder
- redaction notice
- terminal preview status

Audit data should be compact and structured. It should not store full unredacted prompts, raw terminal output, full file bodies, credential values, tokens, cookies, or raw secret-bearing configuration.

## Redaction Expectations

The preview tool should redact secret-shaped input before returning durable preview data.

Document secret source names only when needed, such as an environment variable name. Never include the value.

Do not put these in preview requests, responses, handoffs, logs, or summaries:

- raw passwords
- tokens
- cookies
- bearer values
- API keys
- private keys
- credential-bearing connection strings
- real `.pubxml`, `.pubxml.user`, `.env`, `.pfx`, or publish settings contents

If redaction cannot be trusted, stop before producing durable artifacts.

## Stop Conditions

Stop instead of proceeding when:

- the preview asks for Codex execution
- command execution is hidden inside preview generation
- repository mutation is hidden inside diagnostics or search
- deployment appears without explicit approval
- destructive git actions appear
- a secret value would need to be exposed
- the task is too broad to validate in one slice
- approval status is ambiguous
- background polling or auto-continuation is requested
- production auth, persistence, middleware, or external service changes appear unexpectedly

## Good Narrow Slice Examples

- Create a docs-only readiness review for a preview-only tool; validation is `git diff --check`.
- Add one handoff documenting a completed validation result; no runtime code.
- Implement a preview-only contract with focused tests; no execution path.
- Add a safe `.template` file and canonical inventory doc; no real credential files.
- Validate a deployed guardrail after one explicitly approved deployment attempt.

## Unsafe Slice Examples

- Generate the preview and run Codex automatically.
- If tests pass, commit, push, deploy, and start the next task.
- Reset the branch if validation fails.
- Publish BlogAI and update production auth in the same slice.
- Rewrite all docs, move projects, and delete obsolete files in one cleanup pass.
- Use the deployment password directly in the handoff.
- Convert local/dev auth diagnostics into production auth.

## Overly Broad Orchestration Requests

These should be rejected or split before preview approval:

- "Build the whole ChatGPT to Codex orchestration loop."
- "Create the handoff, run Codex, wait for it, summarize, and continue until done."
- "Add deployment automation with automatic smoke retries."
- "Implement production auth, admin APIs, persistence, and UI."
- "Find everything related and clean it up."

The correct response is to ask for one approved narrow slice with explicit validation and stop conditions.

## Explicit Rejections

The preview workflow rejects:

- autonomous execution
- hidden command execution
- hidden repo mutation
- background task loops
- auto-approval
- silent continuation
- automatic deployment
- destructive git behavior without explicit approval
- raw secret capture
- speculative orchestration

## Why Preview-First Matters

Preview-first keeps the human decision visible before work begins. It lets ChatGPT normalize task shape, expose risk flags, preserve correlation metadata, and redact secret-shaped inputs before Codex receives anything executable.

This preserves trust because the proposal can be inspected before action. It preserves operational clarity because each slice has a defined target, validation plan, and stop condition. It also keeps future automation honest: tooling may reduce formatting friction, but it must not remove the approval gate.
