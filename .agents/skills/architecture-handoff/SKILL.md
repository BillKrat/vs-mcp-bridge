---
name: architecture-handoff
description: Create or update focused session handoffs while preserving SolutionFolder/docs/ARCHITECTURE.md as the architecture source of truth.
---

# Architecture Handoff

## Use When

- Ending a meaningful architecture, validation, security, or diagnostic slice.
- Recording a new resume point for future AI sessions.
- Summarizing completed work, validation state, blockers, and next-step options.

## Workflow

1. Read `AI_START.md`, `SolutionFolder/docs/AI_STOP.md`, `SolutionFolder/docs/ARCHITECTURE.md`, and the latest relevant handoff.
2. Keep the handoff focused on checkpoint, completed work, validation, operating rules, and resume guidance.
3. Update `SolutionFolder/docs/ARCHITECTURE.md` only when current system behavior or architectural invariants changed.
4. Link to durable evidence rather than copying large logs, diagrams, or runbooks.
5. Avoid turning handoffs into roadmaps; include next-step options only when they are grounded in the completed slice.
6. Record branch, commit, and working tree expectations when useful for resumption.

## References

- `AI_START.md`
- `SolutionFolder/docs/AI_STOP.md`
- `SolutionFolder/docs/ARCHITECTURE.md`
- `SolutionFolder/docs/session-handoffs/current-working-mode.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-end-of-day-architecture-handoff.md`
