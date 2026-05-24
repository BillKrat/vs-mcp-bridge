# Preview Diff Ergonomics Plan

## Purpose

Capture the practical preview and diff ergonomics lessons from the first real `bridge_preview_document_update` workflow.

This is a planning document only. It does not implement diff changes, add apply/write capability, redesign preview architecture, or change the current MCP mutation boundary.

## Observed Workflow

The first real preview-only document update workflow succeeded safely:

- direct MCP stdio with the official MCP .NET client worked
- `tools/list` exposed `bridge_preview_document_update`
- the tool generated a deterministic preview for an explicit documentation target
- the target file hash did not change after preview, repeated preview, no-op, or drift calls
- no file mutation happened through MCP
- Codex applied the accepted documentation edit later through normal repo editing
- drift and no-op behavior were observable and structured
- approval, audit, redaction, manifest metadata, and correlation stayed on the existing `BridgeToolExecutor` path

Evidence:

- `SolutionFolder/artifacts/logs/mcp-preview-document-update-real-workflow-20260517.log`
- `SolutionFolder/artifacts/logs/mcp-preview-document-update-real-workflow-20260517.metadata.json`
- `SolutionFolder/docs/diagrams/mcp-preview-document-update-real-workflow-20260517.mmd`
- `SolutionFolder/docs/session-handoffs/2026-05-17-preview-document-update-real-workflow.md`

## Friction Points

The safety shape worked, but real use exposed readability and operator-friction issues:

- Large changed-line counts for small edits: a short inserted section produced `changedLineCount=460` because the current diff treats the operation as a full-document replacement.
- Noisy unified diffs: the preview was deterministic, but the full file appeared as removed and re-added, making the meaningful change harder to spot.
- Full-document replacement semantics: requiring complete `replacementContent` is safe and explicit, but it pushes the diff generator to compare whole document bodies instead of the intended edit region.
- Preview readability: humans need the proposed change, nearby context, target metadata, and safety status quickly; they should not have to scan a whole-document diff for a small insertion.
- Meaningful-change detection: no-op and drift are already structured, but preview output does not yet distinguish small localized changes from broad rewrites in a reader-friendly way.
- Raw MCP invocation ergonomics: an ad hoc raw JSON-RPC Content-Length helper timed out before `initialize`; the official MCP client path worked and should be the preferred repeatable validation path.
- Context-window pressure: large full-document previews can consume chat/tool context quickly, especially when future targets are longer docs or when multiple preview attempts are compared.

## Principles To Preserve

Any improvement must preserve the current safety model:

- deterministic previews for the same input and repository state
- explicit repo-root-relative targets
- explicit expected state by content or hash
- no hidden file discovery
- no hidden mutation
- no apply/write behavior inside preview
- no fuzzy target selection or hidden retries
- human-reviewable output
- structured status for preview, no-op, drift, and invalid requests
- observable request and operation boundaries
- redacted logs and audit metadata
- compatibility with `BridgeToolExecutor` policy, approval metadata, audit, and correlation seams

## Small Future Improvements

The smallest useful improvements are ergonomic output changes, not mutation changes.

### Minimal Line-Based Diff Generation

Replace the current whole-document remove/add diff with a deterministic line-based diff that preserves standard unified diff shape and emits only changed hunks with context.

Requirements:

- same input and file state produce byte-stable output
- unchanged lines outside hunks are omitted
- hunk context is deterministic, for example three lines before and after
- line endings are normalized consistently for diff generation
- changed-line counts reflect actual added/deleted lines, not full document size
- no file writes or apply behavior are introduced

### Diff Summary Statistics

Add structured metadata that helps a caller decide whether a preview is small, broad, or suspicious before reading the diff body.

Candidate fields:

- `addedLineCount`
- `deletedLineCount`
- `modifiedHunkCount`
- `unchangedContextLineCount`
- `diffTruncated`
- `originalLineCount`
- `replacementLineCount`
- `largestHunkChangedLineCount`

These fields should supplement, not replace, `previewOnly`, `expectedMatched`, `noOp`, `changedLineCount`, and `approvalRequiredForApply`.

### Context-Window Diff Truncation

Provide deterministic truncation when a preview is too large for practical review.

Requirements:

- truncation must be explicit in structured metadata
- the result must state which hunks or line ranges were omitted
- truncation must never imply approval or apply safety
- full diff retrieval can remain deferred
- no mutation path is added

### Preview Chunking

For large diffs, consider returning stable chunk metadata so callers can inspect one hunk or chunk at a time.

This should remain read-only preview output. It should not become patch application, patch generation, or hidden retry behavior.

### Semantic And No-Op Detection Improvements

Current no-op detection is exact full-content equality. Future improvements may add structured metadata for common readability cases without changing safety decisions:

- whitespace-only change indicator
- line-ending-only change indicator
- frontmatter-only change indicator for docs that have explicit frontmatter
- heading-only or section-only summary when the diff can be derived deterministically

These should be labels on deterministic comparison output, not AI-generated mutation heuristics.

### Official MCP Client Helper Guidance

Document or provide a repeatable validation helper that uses the official MCP client path rather than ad hoc raw JSON-RPC framing.

The helper should:

- initialize the MCP server over stdio
- list tools
- invoke `bridge_preview_document_update`
- verify file hashes before and after the call
- capture no-op and drift examples
- avoid storing raw full document bodies in durable artifacts by default

This guidance can live near existing MCP validation docs or as a future script slice. It should not introduce apply/write capability.

## Explicitly Deferred

Do not implement these as part of preview ergonomics:

- autonomous patch generation
- automatic apply
- fuzzy patching
- hidden retries against alternate target paths
- AI-generated mutation heuristics
- broad file mutation workflows
- multi-file mutation batches
- approval UX changes
- repository writes through MCP
- production publishing, deployment, cache clearing, or external endpoint calls

## Recommended Next Implementation Slice

The next appropriate implementation slice, if requested, is minimal unified diff generation.

Scope:

- improve the deterministic diff algorithm used by `bridge_preview_document_update`
- keep the existing full-document explicit input contract
- return smaller standard unified diff hunks with stable context
- add focused tests for small insertion, deletion, replacement, no-op, drift, and large-diff truncation if truncation is included
- preserve no-write behavior and all existing mutation safety boundaries

Non-scope:

- apply/write
- range replacement inputs
- patch application
- broad repository workflows
- architecture redesign
