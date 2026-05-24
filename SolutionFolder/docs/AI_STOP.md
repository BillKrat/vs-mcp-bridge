# AI Stop

This is the canonical session closeout document for AI-assisted work in `vs-mcp-bridge`.

If you are an AI assistant ending or pausing a meaningful work session, read this file before wrapping up.

## Purpose

Use this document to preserve session state, avoid losing context, and leave the repository in a condition that can be resumed if the next session starts fresh or if prior chat state is lost or corrupted.

This file complements `AI_START.md`.

- start a session with `AI_START.md`
- end or pause a session with `SolutionFolder/docs/AI_STOP.md`

## Stop Rules

1. Treat repository files as the durable source of truth.
2. Do not rely on chat history as the only record of decisions, validation, or next steps.
3. If behavior, priorities, or the recommended resume point changed, update the durable docs before ending.
4. If the next session would benefit from a clean resume point, create or update a handoff in `SolutionFolder/docs/session-handoffs/`.
5. End the session with no uncommitted work whenever reasonably possible.
6. If validated work was completed, commit it before stopping.
7. If a clean working tree is not possible, explicitly isolate and document the exception so the last successful validated session can still be restored without guesswork.
8. Leave enough detail that a fresh AI session can restart with `AI_START.md` and continue without guesswork.

## Required Closeout Checklist

Before ending a meaningful session, complete this checklist as applicable:

### 1. Update durable state

Update durable docs when any of the following changed:

- current priorities
- validated system behavior
- known limitations or UX gaps
- logging/diagnostic guidance
- the recommended next slice

Typical durable docs to update:

- `AI_START.md`
- `README.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/VS_MCP_BRIDGE_TECHNICAL_ANALYSIS.md`
- `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`

### 2. Update `AI_START.md` when needed

Update `AI_START.md` if any of these changed:

- the core grounding set
- the default or recommended resume handoff
- the current working guidance for the repo
- the expected start/resume prompt examples
- the highest-priority next session target

`AI_START.md` should stay concise and canonical. Do not turn it into a long narrative history.

### 3. Create or update a session handoff

Create or update a file under `SolutionFolder/docs/session-handoffs/` when:

- work is incomplete
- manual testing revealed a new defect or UX issue
- a new validated slice was completed
- the next session needs a precise restart point
- there is any meaningful risk of context loss

A good handoff should capture:

- purpose of the session
- what was completed
- what was validated
- current blockers or known issues
- likely relevant files
- recommended next steps
- a suggested resume prompt

### 4. Validate the work

Perform standard validation appropriate to the session scope.

Typical validation includes:

- run all applicable unit tests for the changed area
- add or update tests if behavior changed
- run a build validation for the affected workspace when code changed
- perform manual validation when the work depends on UX, VSIX behavior, integration behavior, or logging surfacing
- distinguish validated behavior from assumptions still needing confirmation

If no code changed, validate documentation accuracy by reading back the updated docs and handoff files.

### 5. Leave the working tree in a recoverable state

Default expectation:

- end the session with a clean working tree
- commit validated work with the normal `Copilot:` commit prefix before stopping

If that is not possible, explicitly record:

- what remains uncommitted
- why it is intentionally uncommitted
- whether it is validated or still experimental
- how to safely discard it and return to the last successful validated commit

The goal is that the repository can always be restored to the last successful validated session without reconstructing lost work from memory.

### 6. Record the next step explicitly

Before stopping, record:

- the next recommended coding slice
- any constraints that should be preserved
- whether the session should resume from a specific handoff file

## Recovery Goal

A future AI session should be able to do this successfully:

1. read `AI_START.md`
2. read the referenced handoff
3. inspect the current repo state
4. continue the next slice without depending on prior chat history

If that would not work, the closeout is incomplete.

## Recommended End-Of-Session Prompt

A good closeout prompt is:

- `Read SolutionFolder/docs/AI_STOP.md and do the required closeout updates before ending the session.`

## Relationship To `AI_START.md`

- `AI_START.md` is the entry point
- `SolutionFolder/docs/AI_STOP.md` is the exit checklist
- `AI_START.md` should point to the current best resume target
- `SolutionFolder/docs/AI_STOP.md` should be used to keep `AI_START.md` and the handoff trail current

## Latest Session Closeout

Date: 2026-05-08
Branch: `feature/approval-apply-ui-slice`

### What Changed

- preserved the validated placeholder-path fix and its regression tests from the earlier slice
- confirmed the prior App-host and VSIX-host ping trace workflow session was fully completed before the later context-loss issue
- confirmed manual VSIX behavior that `what is the active file` works as expected in the prompt UI
- created `SolutionFolder/docs/session-handoffs/2026-05-08-selected-text-prompt-investigation-handoff.md` for the new stopping point
- updated `AI_START.md` so the next session resumes from the incomplete selected-text prompt investigation

### Validation Performed

- previously validated placeholder-path fix remains documented as:
  - `VsMcpBridge.Vsix.Tests`: 25/25 passed
  - workspace build: successful
- manual VSIX validation confirmed `what is the active file` behaves as expected when no real editor document is active

### Current Working Tree State

- validated but uncommitted changes remain in the working tree
- modified files currently include code/doc updates for the placeholder-path fix plus AI/handoff documentation updates
- if the session stops before commit, resume from `SolutionFolder/docs/session-handoffs/2026-05-08-selected-text-prompt-investigation-handoff.md`

### Recommended Next Step

- reproduce `what is the selected text` in the Experimental Instance with a known selection, capture the visible response and logs, and compare the observed flow against presenter routing and `VsService.GetSelectedTextAsync`
