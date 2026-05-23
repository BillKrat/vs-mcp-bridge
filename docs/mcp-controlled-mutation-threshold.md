# MCP Controlled Mutation Threshold

## Purpose

Define the architectural threshold between current MCP-assisted analysis/search workflows and any future MCP-assisted repository mutation workflow.

This is planning guidance only.
It does not add MCP mutation tools, edit/apply tools, runtime code, approval behavior changes, or autonomous editing.

## Current State

Current MCP diagnostics support analysis, search, and observability:

- `bridge_get_tool_inventory`
- `bridge_select_repo_documents`
- `bridge_regex_text_search`
- `bridge_bm25_text_search`
- `bridge_preview_document_update`
- VS/host diagnostic and proposal tools already documented in `docs/ARCHITECTURE.md`

Repository edits are not currently performed through MCP mutation tools.
Codex edits files directly through the normal repository workflow, with local diffs, validation, commit, and push.

`bridge_preview_document_update` is mutation-adjacent but remains below the mutation threshold.
It can read one explicit repo-root-relative target and generate deterministic before/after preview metadata plus a unified diff, but it cannot write, apply, create, delete, rename, format, publish, deploy, call production endpoints, call the VSIX named pipe, or execute shell commands.
It exists to exercise manifest, capability, policy, approval metadata, redaction, audit classification, and correlation seams before any write-capable MCP tool exists.

The current MCP search workflow boundary is explicit:

- document selection returns metadata only
- regex and BM25 search require explicit caller-provided text/documents
- search tools do not crawl files, read paths, mutate files, or store indexes
- executable bridge search tools run through `BridgeToolExecutor`

`BridgeToolExecutor` already provides the foundation that future mutation-capable tools would have to use:

- descriptor/manifest metadata
- capability metadata
- policy evaluation
- approval requirement metadata and approval service seam
- redaction before payload-oriented logs/audit metadata
- audit envelope emission
- request and operation correlation
- structured success/failure results

Those seams are necessary but not sufficient for repository mutation.

## Mutation Threshold

An MCP tool crosses the mutation threshold when it can create, update, delete, rename, move, format, publish, deploy, or otherwise change repository files, runtime state, external systems, or production content.

Before any MCP mutation tool is introduced, the tool must have all of these properties:

- explicit tool manifest with stable id, name, version, description, category, and risk hints
- explicit capability metadata that describes the kind of mutation requested
- approval requirement set to required for any write/apply path
- audit classification that identifies mutation category, severity, risk, and outcome
- deterministic target selection with explicit paths/targets supplied by the caller
- before/after preview before any write
- dry-run or preview mode that does not mutate state
- no silent writes
- structured success/failure result with request id and operation id
- redacted logs and audit metadata
- durable evidence for meaningful runs
- rollback or recovery guidance where practical

## Candidate Future Tools

These are possible future tool names, not implementation commitments:

| Candidate Tool | Intended Role | Mutation Status |
| --- | --- | --- |
| `bridge_preview_document_update` | Produce a deterministic before/after diff for explicit document updates. | Implemented preview-only; below mutation threshold. |
| `bridge_apply_document_update` | Apply a previously previewed and approved document update. | Mutation; deferred. |
| `bridge_create_handoff` | Create a structured handoff from explicit caller-provided content. | Mutation; deferred. |
| `bridge_annotate_evidence_header` | Add or update a lightweight evidence classification header on explicit files. | Mutation; deferred. |
| `bridge_apply_patch` | Apply a caller-provided patch with validation and approval. | High-risk mutation; deferred until stronger proof exists. |

## Non-Negotiable Safety Rules

Future mutation-capable MCP tools must follow these rules:

- no autonomous broad edits
- no hidden file crawling before mutation
- no mutation without explicit paths or targets
- no mutation without approval
- no mutation hidden inside search, inventory, or diagnostic tools
- no secrets in patches, logs, previews, prompts, or audit metadata
- no production endpoint calls as a side effect of repository mutation tools
- no deployment, publishing, app recycle, or runtime cache clear inside document-editing tools
- all mutations must be traceable through request id, operation id, preview, approval, result, and durable evidence when meaningful

## Required Preview Shape

Any future write-capable path should first produce a preview result containing:

- tool id and manifest summary
- request id and operation id
- explicit targets
- target existence and current-state checks
- proposed before/after diff
- expected file count and changed line count
- safety classification
- whether approval is required
- whether the request is dry-run/preview-only
- redaction status
- validation warnings

The preview must be deterministic for the same input and repository state.

## Approval Boundary

Approval must happen after preview and before mutation.

Approval should include:

- exact tool id and version
- exact target list
- exact diff or patch to apply
- risk/audit classification
- request id and operation id
- validation result
- statement of whether rollback is available

Approval should not be inferred from a prior search, BM25 ranking, document selection result, chat message, or branch state.

## Durable Evidence

Meaningful mutation workflows should preserve enough evidence to reconstruct what happened:

- input summary
- selected targets
- preview diff
- approval decision
- apply result
- rollback/recovery result when applicable
- logs with request/operation correlation
- Mermaid sequence diagram when the workflow changes platform behavior or operational practice

Evidence artifacts should avoid raw secrets and should not duplicate large file bodies unless the content itself is the reviewed artifact.

## Preview-Only Slice

The first mutation-adjacent slice is preview-only:

- tool name: `bridge_preview_document_update`
- no write capability
- explicit target paths only
- no file crawling
- caller-provided replacement content or patch content
- deterministic diff output
- validates current file state
- exercises manifest, capability, policy, approval metadata, audit classification, redaction, and correlation without changing files

This slice proves the safety and observability shape before any apply/write tool exists.
The detailed design for that preview-only tool is `docs/preview-only-document-update-tool-design.md`.
Practical diff readability improvements are tracked separately in `docs/preview-diff-ergonomics-plan.md`; those improvements must remain preview-only and must not add apply/write behavior.

## Deferred

Do not implement in this planning slice:

- mutation tools
- apply/edit tools
- autonomous editing
- background indexing
- hidden persistence
- broad repo mutation
- production publishing automation
- BlogAI auth or deployment behavior
- changes to approval UX
- changes to MCP transport

The current rule remains: MCP diagnostics may help select, search, rank, and explain explicit evidence; repository mutation remains outside MCP mutation tooling until the threshold above is deliberately implemented and validated.
