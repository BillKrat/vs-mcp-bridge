# AI Workflow

This repository uses two distinct AI roles:

## ChatGPT

ChatGPT is responsible for:

- defining feature slices
- defining constraints
- explaining architecture intent
- reviewing implementation results
- answering questions and clarifying intent

## Codex

Codex is responsible for:

- executing the exact approved slice
- making minimal code changes
- adding or updating tests as instructed
- reporting what changed and test results

## Codex Execution Rules

Codex should not:

- provide high-level plans
- ask to proceed
- expand scope
- introduce new architecture

If blocked, Codex should ask one specific blocking question.

Otherwise, Codex should:

- implement directly
- add or update tests as instructed
- report changes and test results

## Workflow

1. ChatGPT defines a slice.
2. Instructions are given to Codex.
3. Codex implements the approved slice directly.
4. Results are reviewed in ChatGPT.

This separation is intentional and enforced to maintain:

- architectural control
- minimal scope
- predictable changes

## Observability Requirement

At any point in application development, important workflows should remain testable as end-to-end observed runs. The expected evidence is:

- a repeatable automation or documented manual trigger with automated capture steps
- correlated logs that show the request path across meaningful boundaries
- a Mermaid sequence diagram generated from observed artifacts
- a clear comparison between observed flow and intended code flow

New code should follow the existing logging pattern when applicable so AI sessions can quickly triage failures and isolate the first missing boundary instead of reverse-engineering black-box behavior.

Concrete examples of this standard are maintained in:

- `docs/app-host-ping-trace-workflow.md`
- `docs/vsix-host-ping-trace-workflow.md`
- `docs/vsix-host-selected-text-trace-workflow.md`
