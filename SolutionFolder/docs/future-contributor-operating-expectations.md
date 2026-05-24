# Future Contributor Operating Expectations

## Purpose

Define behavioral expectations for any future contributor working in `vs-mcp-bridge`, including humans, ChatGPT, Codex, Copilot, future agents, and future tools.

This document is operational guidance. It does not add runtime code, deployment behavior, tool implementation, or new automation authority.

## Core Principles

- Humans remain the approval gate.
- Narrow slices are preferred over broad rewrites.
- Durable evidence is preferred over chat memory.
- No hidden mutation behavior.
- Summarize before proceeding.
- Preserve operational clarity.
- Preserve auditability and request/operation correlation.
- Do not expand speculative infrastructure ahead of proven need.
- Deployment must be reproducible.
- Secrets must remain externalized and redacted.
- Architectural boundaries matter more than speed.

## Required Workflow

Every meaningful session or slice should follow this sequence:

1. Establish the checkpoint.
2. Read the minimal grounding docs.
3. Define the slice.
4. Define constraints and non-goals.
5. Execute only the approved scope.
6. Validate with the smallest command set that proves the slice.
7. Produce durable evidence if meaningful state changed.
8. Summarize what changed and what was validated.
9. Stop when scope expands unexpectedly.

If the working tree is not clean, classify existing changes before editing. Do not overwrite or revert unrelated work unless explicitly asked.

## Checkpoint Expectations

Start from a concrete checkpoint:

```text
Current checkpoint:
main == origin/main
HEAD: <short-sha> <commit subject>
working tree clean
```

If the checkpoint is wrong, stop and report the mismatch before changing files.

## Grounding Expectations

Read only what is needed for the slice, but always prefer repository files over chat memory.

Default grounding:

- `AI_START.md`
- `AGENTS.md`
- `SolutionFolder/docs/ARCHITECTURE.md` when architecture or runtime behavior may be touched
- the newest relevant handoff under `SolutionFolder/docs/session-handoffs/`
- targeted design or validation docs for the active area

For short sessions, use `SolutionFolder/docs/session-slice-operating-template.md`.

## Validation Expectations

Validation should match blast radius:

- docs-only: `git diff --check`
- shared logic: relevant tests plus `git diff --check`
- bridge tool behavior: tests, builds, and durable evidence when behavior changes
- deployment: explicit pre-checks, one approved attempt, smoke checks, and non-secret reporting

Do not hide validation failures. Report non-secret warnings and errors.

## Handoff Expectations

Create or update durable handoff evidence when:

- behavior changed
- deployment state changed
- an operating boundary changed
- validation results change what future sessions should assume
- the next session would otherwise need chat history

Handoffs should be concise and operational:

- checkpoint
- inputs reviewed
- what changed or was learned
- validation performed
- stable behavior
- known gaps
- next smallest slice
- explicitly deferred scope

## AI-Specific Expectations

AI contributors must:

- propose, validate, and summarize
- preserve user decision points
- surface risks and stop conditions
- stop when scope expands unexpectedly
- avoid self-expanding a narrow task into a broad roadmap
- avoid silently continuing after execution
- keep secrets out of prompts, summaries, logs, commits, and durable artifacts
- distinguish current behavior from future design
- distinguish preview/proposal from mutation
- keep deployment, git mutation, and production-auth work behind explicit approval

AI contributors must not:

- infer approval from clean git state
- infer approval from prior chat context
- continue into a second slice without a user decision
- hide mutation inside diagnostics, search, or preview tools
- make runtime or deployment changes during docs-only slices
- print secret values

## Contributor Anti-Patterns

Avoid these patterns:

- giant slices
- hidden side effects
- autonomous mutation loops
- production auth creep
- architecture drift
- speculative automation
- deployment without validation
- undocumented operational assumptions
- broad refactors inside narrow validation work
- adding persistence, middleware, auth, or external services because they seem adjacent
- treating local/dev diagnostics as production behavior
- treating a preview as permission to apply
- treating a generated task as permission to execute

## Deployment Expectations

Deployment is not routine background work.

Before deployment:

- confirm deployment was explicitly approved
- confirm repo path exists
- confirm branch and working tree
- confirm required environment variables are present without printing them
- build the target
- document command shape with secrets masked

After deployment:

- record exit code
- record non-secret warnings/errors
- smoke the expected URLs
- record status codes and required rendered markers
- update durable evidence if deployment state changed

Do not commit publish profiles, publish settings, `.pubxml.user`, `.env`, or credential files.

## Secrets And Redaction

Secrets must remain externalized and redacted.

Rules:

- do not print passwords, tokens, cookies, bearer values, API keys, or raw credentials
- record secret source names only when useful
- use masked command shapes
- redact logs and audit metadata before making them durable
- use structured secret references when a future tool needs a secret boundary
- stop if a task requires exposing a secret value

## Architecture Boundary Expectations

Preserve established boundaries:

- `BridgeToolExecutor` remains the bridge tool execution, policy, approval, redaction, audit, and correlation boundary.
- MCP diagnostics remain explicit-input and non-mutating unless a future approved design changes that.
- Preview-only tools must not grow apply/write behavior.
- Proposal/apply workflows remain approval-gated.
- BlogAI local/dev diagnostics are not production auth.
- Auth-admin belongs to the auth service/API host boundary, not BlogAI Razor pages.
- MEF tool support remains discovery-only unless a future explicit slice changes it.

## When To Stop

Stop rather than continue when:

- scope expands beyond the approved slice
- validation fails
- current state differs from the checkpoint
- required secrets are missing or would need to be exposed
- deployment would be required but was not approved
- a task crosses from preview/proposal into mutation
- production auth, persistence, middleware, or external systems enter a slice unexpectedly
- the next action would be destructive or hard to roll back
- durable evidence is needed before future work can be trusted

## When NOT To Automate

Do not automate when:

- the workflow has not been proven manually
- the stop conditions are unclear
- approval boundaries are unclear
- the action mutates repo, git, deployment, or production state
- secrets are involved and redaction is not proven
- the automation would hide review
- background loops or unattended continuation would be required
- the main value is a human decision

Prefer preview/proposal and durable evidence before automation.

## Practical Closeout

Closeouts should be short and concrete:

- files changed
- validation run
- commit hash if pushed
- final git status
- known blockers if any

Avoid UI-driven completion flows. Use terminal git commands and report the final repository state plainly.
