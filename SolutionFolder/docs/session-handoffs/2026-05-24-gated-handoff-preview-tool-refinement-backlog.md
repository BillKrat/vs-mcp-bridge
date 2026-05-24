# Gated Handoff Preview Tool Refinement Backlog

## Checkpoint

- branch: `main`
- starting HEAD: `323a427 Add gated handoff preview tool usage guide`
- starting state: `main == origin/main`, working tree clean
- scope: docs-only refinement backlog for the preview-only gated handoff proposal tool

## Inputs Reviewed

- `AI_START.md`
- `SolutionFolder/docs/gated-handoff-preview-tool-usage-guide.md`
- `SolutionFolder/docs/session-handoffs/2026-05-24-gated-handoff-preview-real-workflow.md`
- `SolutionFolder/docs/session-handoffs/2026-05-17-gated-handoff-preview-tool-validation.md`

## Current Observations

The preview-only gated handoff tool is usable for real handoff previews with minimal editing.

Current validated behavior remains acceptable:

- the compiled catalog discovers `bridge.gatedHandoffPreview`
- inventory includes the tool
- preview output includes scoped task text, constraints, validation checklist, expected artifacts, stop conditions, risk flags, approval reminder, redaction notice, and correlation metadata
- no Codex execution occurs
- no command execution occurs
- no repo mutation occurs
- no deployment occurs
- no background workflow starts

Known observations from the real workflow validation:

- `LocalOnlyFiles` is preserved when caller-provided, not inferred automatically
- risk detection is conservative and can flag terms from explicit non-goals such as "no command execution" or "do not execute Codex"
- the operational usage guide now exists and defines how the preview should be reviewed before Codex receives a task
- no urgent implementation refinement is required

## Potential Refinements

### 1. Better Risk Flag Classification

Potential improvement:

- distinguish "requested risky action" from "explicitly prohibited risky action"

Example:

- requested: "run deployment after generating the preview"
- prohibited: "no deployment"

The current conservative behavior is safe because it surfaces risk. A future refinement could make review less noisy by preserving both the risky term and its prohibition context.

Do not implement until repeated review friction shows that the current conservative flags slow down handoff approval.

### 2. Optional Local-Only-File Hinting

Potential improvement:

- add optional local-only-file hinting when the caller provides local-only context or points at known inventory docs

Constraints:

- no hidden repo crawling
- no implicit filesystem inventory
- no reading real credential-bearing files
- no secret extraction
- no inference from ignored local files unless the caller explicitly supplies safe metadata

The safe first version would use only caller-provided context and known tracked docs such as `SolutionFolder/docs/local-only-files.md`.

### 3. Improved Preview Formatting

Potential improvement:

- optimize `scopedTaskText` for direct copy/paste into Codex
- keep checkpoint, read-first docs, constraints, validation, secret rules, and commit expectations in consistent order
- reduce manual cleanup while preserving human review

This should remain formatting-only. It must not add execution authority or infer additional scope.

### 4. Validation Checklist Normalization

Potential improvement:

- preserve user intent while producing consistent validation sections
- separate docs-only validation, build/test validation, deployment validation, and secret-safety validation when provided

The tool should not invent validation commands beyond obvious formatting of caller-provided requirements unless a future design explicitly allows safe suggestions.

### 5. Audit And Correlation Display Refinement

Potential improvement:

- make correlation metadata easier to trace across preview, approved handoff, Codex result, and ChatGPT summary
- display request id, operation id, and correlation id in a stable block
- make missing/generated metadata visually distinct from caller-provided metadata

This would improve operational traceability without changing tool authority.

## Decision

Do not implement refinements now.

Continue using the preview-only gated handoff tool operationally. The current behavior is safe, useful, and validated. The known gaps are minor contract-refinement candidates, not blockers.

Prioritize refinements only after repeated friction appears in real handoff use.

## Deferred Scope

Do not add in this backlog slice:

- runtime code
- tool contract changes
- Codex execution
- command execution
- repo mutation behavior
- deployment behavior
- background workflow
- automatic continuation
- hidden repo crawling
- secret file inspection

## Smallest Future Implementation Candidate

If repeated friction justifies implementation, the smallest future slice should be:

1. choose exactly one refinement
2. update the preview contract or formatting in that one area only
3. add focused tests proving no side effects were introduced
4. validate redaction, correlation, and no-execution behavior still hold

The likely first implementation candidate is better risk flag classification, because it directly addresses the observed false-positive noise from explicit non-goals while preserving conservative safety.
