# Evidence Classification Guidance

## Purpose

Make MCP search and BlogAI investigation results easier to interpret by distinguishing canonical/current source from preserved historical and diagnostic evidence.

This is lightweight human-readable guidance only.
It does not add a database, metadata service, background index, automated classifier, hidden cache, runtime behavior, or MCP transport behavior.

The goal is to keep explicit deterministic workflows clear:

- choose evidence categories intentionally
- record which categories were searched
- preserve which category produced each important finding
- avoid treating historical hits as current source-of-truth hits

## Categories

| Category | Meaning | Typical Locations | Use In Search Workflows |
| --- | --- | --- | --- |
| `canonical-current` | Current repo source intended to represent the desired state. | `docs/blogs/posts/<slug>/post.json`, `docs/blogs/posts/<slug>/content.html`, current architecture docs. | Search first when proving whether a marker remains in current source. |
| `operational-handoff` | Session resume context, conclusions, validation status, and next actions. | `docs/session-handoffs/*.md`. | Search to recover why work moved a certain direction, but do not treat as source content. |
| `historical-evidence` | Preserved older state, migration input, database export, or comparison baseline. | `docs/blogs/source-of-truth/db-export-*`, preserved export folders, legacy reference docs. | Search intentionally when comparing current state to prior state. |
| `preserved-before-update` | Snapshot captured immediately before a controlled update. | `before-update` export folders, prepublish compare docs, before/after update folders. | Use to prove what changed and what did not. |
| `rendered-failure-evidence` | Captured rendered-site or runtime behavior showing a failure or mismatch. | `final-rendered-*`, `*failure*`, stale chrome reports. | Use to diagnose runtime/cache/rendering behavior; do not infer canonical source from it. |
| `diagnostic-trace` | Logs, metadata, sequence diagrams, and validation traces from observed tool/runtime workflows. | `artifacts/logs/*.log`, `artifacts/logs/*.metadata.json`, `docs/diagrams/*.mmd`, trace handoffs. | Use to prove what ran, with request ids and observed boundaries. |
| `transient-generated` | Temporary local output, scratch harnesses, generated build/test artifacts, or untracked machine-local files. | temp folders, build output, ignored artifacts. | Do not cite as durable evidence unless promoted into a tracked artifact with context. |

## Folder Semantics

Use existing folder location as the first classification signal:

- `docs/blogs/posts/` is `canonical-current` for materialized repo-backed blog posts.
- `docs/blogs/source-of-truth/` is preserved source-of-truth evidence, usually `historical-evidence` or `preserved-before-update` depending on the subfolder.
- `docs/blogs/source-of-truth/db-export-*` is `historical-evidence` unless a workflow explicitly redefines it as the current database baseline.
- `docs/blogs/source-of-truth/widget-settings/*before*` or dated pre-update folders are `preserved-before-update`.
- `docs/session-handoffs/` is `operational-handoff`.
- `docs/diagrams/` is `diagnostic-trace`.
- `artifacts/logs/` is `diagnostic-trace`.
- generated temp directories and ignored build outputs are `transient-generated`.

When folder semantics are enough, do not add extra metadata.
When a file mixes categories or is likely to be searched later, add a short marker in the file header.

## Lightweight Header Markers

For new docs that are likely to appear in MCP search results, prefer a tiny header block near the top:

```markdown
Evidence category: operational-handoff
Evidence status: durable
Source authority: diagnostic context, not canonical content
```

Use only the fields that help the reader.
Do not turn ordinary Markdown docs into complex manifests.

Recommended fields:

- `Evidence category`: one category from this guide.
- `Evidence status`: `durable`, `preserved`, `transient`, or `superseded`.
- `Source authority`: short plain-language description of what the file can and cannot prove.
- `Related canonical source`: optional path when the file is evidence about a canonical source.

## Filename Conventions

Use filenames to reinforce category:

- `*-handoff.md` for `operational-handoff`
- `*-trace-YYYYMMDD.log`, `*.metadata.json`, and `*-trace-YYYYMMDD.mmd` for `diagnostic-trace`
- `*-before-*`, `before-update`, or `prepublish-*` for `preserved-before-update`
- `*-failure-*`, `final-rendered-*`, or `stale-*inspection*` for `rendered-failure-evidence`
- dated export folders such as `db-export-YYYYMMDD` for `historical-evidence`

Do not rename existing durable artifacts just to match this guidance.
Add a header note or README entry instead when clarification is needed.

## README And Index Guidance

Folder READMEs should explain category semantics once instead of repeating metadata in every file.

Good README/index entries answer:

- which files are canonical/current source
- which files are preserved historical evidence
- which files are before/after update evidence
- which files are rendered failure or diagnostic traces
- which files should not be treated as publishable source

For BlogAI/blog work, `docs/blogs/README.md` should remain the primary index for blog source-of-truth and publishing review workflows.

## MCP Workflow Guidance

For future MCP search diagnostics:

1. Search `canonical-current` sources first when testing whether a marker remains in current source.
2. Search `historical-evidence`, `preserved-before-update`, and `rendered-failure-evidence` intentionally and label those result groups.
3. Use `bridge_select_repo_documents` category hints to preserve expected evidence class when assembling selected documents.
4. Use file-per-entry inputs when category attribution matters.
5. In handoffs, state whether important matches came from canonical/current source or historical/preserved/diagnostic evidence.
6. If fallback `rg` is used, record the same category grouping and fallback status.

For BlogAI stale shared chrome/cache work, this distinction is critical:

- a stale marker hit in `docs/blogs/posts/**` means canonical source needs review
- a stale marker hit only in handoffs, prepublish reports, rendered failure evidence, or preserved before-update exports confirms historical/diagnostic evidence, not current canonical content

## What Stays Manual

Keep these human decisions explicit:

- choosing which evidence categories are relevant
- deciding whether historical evidence should be searched at all
- deciding whether a hit is actionable or merely preserved context
- promoting transient output into durable artifacts
- updating a handoff when a category distinction changes the recommended next action

## Deferred

Do not add:

- evidence database
- metadata service
- background crawler
- automatic classifier
- hidden search index
- embedding store
- runtime category enforcement
- mutation or deployment behavior

If category needs become repetitive, the next lightweight step should be a checked-in manifest or template, not a service.
