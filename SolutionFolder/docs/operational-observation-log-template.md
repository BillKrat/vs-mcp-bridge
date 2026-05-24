# Operational Observation Log Template

## Purpose

Standardize how workflow friction and operational observations are captured before implementing more automation, process changes, or tooling refinements.

Use this template to record what happened, where it happened, and how often it appears. Do not turn a single observation into automation by default.

> Do not automate based on a single observation.

## Observation Template

```markdown
# Operational Observation: <short title>

## Required Fields

- date/time:
- session objective:
- repo checkpoint/commit:
- workflow step where friction occurred:
- involved tools/actors:
  - ChatGPT:
  - Codex:
  - vs-mcp-bridge:
  - deployment:
  - local environment:
  - documentation:
- friction category:
  - onboarding
  - handoff formatting
  - validation
  - deployment
  - environment
  - correlation/audit
  - repo structure
  - approval workflow
  - local-only configuration
  - orchestration ergonomics
- impact:
  - low
  - medium
  - high
- frequency:
  - one-time
  - repeated
  - systemic
- workaround used:
- durable evidence exists:
  - yes/no
  - link/path:
- recommended action:
  - no action
  - docs clarification
  - process refinement
  - tooling refinement
  - architecture review

## Notes

- observation:
- suspected cause:
- what was explicitly not changed:
- stop condition, if any:
- follow-up threshold:
```

## Field Guidance

`date/time`: Use local date/time when the observation happened.

`session objective`: State the narrow slice being attempted, not the whole project context.

`repo checkpoint/commit`: Record branch alignment and short commit hash when relevant.

`workflow step where friction occurred`: Name the phase: checkpoint, read-first, preview, execution, validation, staging, commit, push, deployment, smoke, or handoff.

`involved tools/actors`: Mark only the actors actually involved. Avoid blaming tools; capture boundaries.

`friction category`: Choose one primary category. Add a secondary category only when it changes the recommended action.

`impact`: Use practical impact:

- low: minor editing, small delay, no safety risk
- medium: repeated delay, confusing recovery, validation rerun, or unclear ownership
- high: safety boundary risk, failed deployment, secret exposure risk, data loss risk, or blocked work

`frequency`: Use `one-time` until the same pattern appears again.

`workaround used`: Record what got the session moving without turning it into the permanent design.

`durable evidence exists`: Link to docs, handoffs, trace artifacts, logs, commits, or validation output when available.

`recommended action`: Pick the smallest action category. Prefer `no action` or `docs clarification` for one-time low-impact friction.

## Example Observations

### Y: Path Versus UNC Path Drift

- workflow step: repo pre-check
- involved tools/actors: local environment, Codex, documentation
- friction category: environment
- impact: medium
- frequency: repeated if both `Y:\vs-mcp-bridge` and `\\Mac\Dev\vs-mcp-bridge` appear in current workflows
- workaround used: confirm `\\Mac\Dev\vs-mcp-bridge` exists and use UNC path when mapped drive is unavailable
- durable evidence exists: session handoffs documenting UNC path requirements
- recommended action: docs clarification

Observation: this is valid operational friction. It affects reproducibility and should be documented, but it does not justify path-discovery automation unless it keeps recurring.

### WebDeploy Environment Variable Visibility

- workflow step: deploy pre-check
- involved tools/actors: deployment, local environment, documentation
- friction category: deployment
- impact: high
- frequency: repeated if deploy retries fail because the current shell lacks the expected variable
- workaround used: confirm `$env:AdventuresOnTheEdgeDP` is present without printing it before any approved deploy attempt
- durable evidence exists: deployment handoffs and local-only file inventory
- recommended action: process refinement

Observation: this is valid operational friction with a safety boundary. It supports explicit pre-checks and redaction rules, not automatic secret probing or deploy retries.

### SolutionFolder Reference Drift

- workflow step: post-move reference audit
- involved tools/actors: documentation, repo structure, Codex
- friction category: repo structure
- impact: medium
- frequency: repeated during and after the folder consolidation
- workaround used: audit current references and fix only obvious broken links
- durable evidence exists: SolutionFolder reference audit handoff
- recommended action: docs clarification

Observation: this was valid cleanup friction. The correct response was targeted link auditing, not broad historical rewrite or deletion of evidence.

### Preview-Tool Risk Over-Flagging

- workflow step: handoff preview review
- involved tools/actors: ChatGPT, vs-mcp-bridge, Codex, documentation
- friction category: orchestration ergonomics
- impact: low
- frequency: one-time until repeated in future handoffs
- workaround used: human reviewer interprets risk flags as conservative review signals
- durable evidence exists: real workflow validation handoff and refinement backlog
- recommended action: no action

Observation: this is valid friction but not yet enough to justify tool changes. Repeated friction may justify better classification between requested risky actions and explicitly prohibited risky actions.

### Local-Only File Survivability

- workflow step: fresh-clone recovery simulation
- involved tools/actors: local environment, documentation, deployment
- friction category: local-only configuration
- impact: medium
- frequency: one-time until a future developer rebuilds the environment
- workaround used: canonical inventory plus safe `.template` file
- durable evidence exists: local-only file inventory and recovery validation handoff
- recommended action: no action

Observation: this is valid survivability work. Additional templates should wait for a repeated onboarding gap.

## Valid Operational Friction

Valid friction is observable, bounded, and tied to a workflow step.

Examples:

- a validation command fails for a reproducible reason
- a docs pointer is stale after a known move
- a required environment variable is missing in the active shell
- a preview result requires repeated manual cleanup before Codex can use it
- an approval boundary is unclear to a human reviewer
- a local-only file cannot be recreated from tracked guidance

Valid friction should be captured before proposing tooling.

## Premature Optimization

Premature optimization appears when one inconvenience becomes a proposed system.

Examples:

- adding path-discovery automation after one mapped-drive failure
- adding execution tooling because copy/paste was mildly annoying once
- creating templates for every ignored file without a concrete onboarding need
- rewriting historical docs just because paths are old but still intentionally historical
- adding broad validation orchestration before manual validation patterns stabilize

Recommended action is usually `no action` or `docs clarification`.

## Dangerous Automation Pressure

Dangerous pressure appears when the proposed response would hide decisions or cross safety boundaries.

Examples:

- automatically retrying deployment when credentials are missing
- probing or printing secret values to make setup easier
- running Codex immediately after generating a preview
- continuing to the next slice after a successful commit
- adding background loops to poll, summarize, and continue
- resetting, cleaning, deleting, or force-pushing to recover from workflow friction
- hiding repo mutation inside diagnostics, search, preview, or inventory tools

Recommended action is `architecture review` or stop-and-ask, not automation.

## Review Threshold

Before implementing tooling from observations, require:

- at least two similar observations, or one high-impact safety issue
- durable evidence for the pattern
- a clear owner boundary
- a narrow proposed change
- explicit non-goals
- validation plan
- stop conditions

If those are missing, keep observing.
