# MCP Search Workflow Ergonomics Gap List

## Purpose

Capture the practical friction found while using MCP search diagnostics against the real BlogAI stale shared chrome/cache investigation.

This is planning guidance only. It does not add runtime code, MCP tools, file crawling, mutation, indexing, transport changes, BlogAI features, auth, or deployment behavior.

The current philosophy stays intact:

- small explicit-input tools
- caller-selected documents
- deterministic workflows
- observable request and operation correlation
- no opaque repo crawling or hidden AI index
- all executable bridge tools remain behind `BridgeToolExecutor`

## Observed Baseline

The first real BlogAI MCP search workflow successfully used:

- `bridge_get_tool_inventory`
- `bridge_select_repo_documents`
- `bridge_regex_text_search`
- `bridge_bm25_text_search`

The workflow searched a bounded caller-built evidence set for the stale shared chrome/cache issue.
It confirmed the prior manual and `rg` findings while avoiding fallback search.

Evidence:

- `docs/session-handoffs/2026-05-16-blogai-mcp-search-workflow-findings.md`
- `docs/session-handoffs/2026-05-16-blogai-doc-selection-search-workflow.md`
- `artifacts/logs/blogai-mcp-search-workflow-20260516.log`
- `artifacts/logs/blogai-doc-selection-search-workflow-20260516.log`

## Practical Gaps

| Gap | Current Workaround | Smallest Useful Improvement | Explicitly Deferred |
| --- | --- | --- | --- |
| Manual explicit-entry assembly | The caller manually chooses files, reads content, builds entries/documents, and records labels. | Add a repo-owned explicit document selection helper that reads a small manifest and emits caller-side entries for MCP search. | MCP-side file crawling, arbitrary path reads, autonomous repo scans. |
| No reusable MCP search runner | Each workflow uses temporary direct MCP stdio glue to call inventory, regex, BM25, and write evidence. | Add a repo-owned runner for explicit-input MCP search sessions that emits log and metadata artifacts consistently. | New MCP transport, new server architecture, hidden persistent service. |
| Entry-level attribution can hide file-level context | Large aggregates, such as canonical posts, return matches by entry unless the caller splits files manually. | Encourage file-per-entry manifests and have the runner preserve entry id, label, source path, and character count. | MCP tools reading file paths themselves or crawling directories. |
| No trace-bundle inventory helper | Agents manually remember which logs, diagrams, metadata files, and handoffs belong together. | Add a helper or manifest convention that lists trace bundle members by run name. | Full artifact database, background indexing, opaque evidence store. |
| No canonical-vs-historical evidence tagging | Agents infer canonical, historical, rendered-failure, widget, source, and handoff categories from filenames and docs. | Add lightweight tags in explicit manifests, for example `canonical`, `historical-export`, `rendered-failure`, `local-source`, `handoff`. | Automated classification that silently decides evidence authority. |
| No manifest/category filtering | Agents manually select subsets such as canonical posts, widget settings, rendered-route reports, or local BlogAI source. | Add manifest filtering by explicit category tags so the caller can build targeted entries reproducibly. | Broad query languages, hidden repo inventory scans, dynamic policy engines. |
| No search-session persistence format | Logs and handoffs preserve outcomes, but there is no small reusable session input/output contract. | Define a lightweight search-session bundle with manifest, query list, selected tool path, result summaries, and fallback status. | Persistent search database, background cache, embedding store. |
| BM25 results lack snippets or hit explanation | The workflow uses ranking to orient, then reads source documents manually. | Add caller-side snippet extraction in the runner after BM25 returns ranked document ids. | Changing `bridge_bm25_text_search` into an opaque summarizer or semantic index. |
| Fallback path recording is manual | The agent writes whether MCP or `rg` was used in the handoff. | Make the runner record `toolPath=mcp` or `toolPath=fallback-rg` in metadata. | Silent fallback that hides which path produced evidence. |
| Discovering canonical versus historical docs takes time | Agents read `docs/blogs/README.md` and prior handoffs, then choose files manually. | Add small curated manifests for common evidence sets, such as BlogAI canonical posts, widget settings, rendered-route reports, and cache/auth source. | Automatic repo-wide source-of-truth inference. |

## First Implemented Helper

`bridge_select_repo_documents` now covers the smallest part of the manual explicit-entry assembly gap.

It accepts caller-supplied repo-root-relative include patterns, optional exclude patterns, optional `maxFiles`, and optional category hints.
It returns deterministic metadata only:

- relative path
- source include pattern
- optional category hint
- file size
- line count when lightweight

It does not:

- read or return file bodies for search
- rank relevance
- call regex or BM25
- execute bridge tools
- mutate files
- build a hidden cache or index
- accept arbitrary absolute paths
- allow broad whole-repo wildcard selection such as `**/*`

The caller still decides which selected files to read and which explicit `entries` or `documents` to pass into `bridge_regex_text_search` or `bridge_bm25_text_search`.

## First Combined Workflow Result

The first BlogAI workflow using document selection plus MCP regex/BM25 search selected 90 deterministic repo documents from explicit patterns:

- `docs/blogs/**/*.md`
- `docs/blogs/posts/**/*.html`
- `docs/blogs/posts/**/*.json`
- `docs/session-handoffs/*blogai*.md`
- `docs/session-handoffs/*BlogAI*.md`

The selected set preserved file-per-entry attribution across:

- 53 blog docs
- 16 canonical post content files
- 16 canonical post metadata files
- 5 BlogAI handoffs

The workflow confirmed the prior stale chrome/cache finding without fallback `rg`:

- the stale `feature/approval-apply-ui-slice` marker matched diagnostic, prepublish, and handoff evidence
- the marker did not match canonical post content or metadata
- BM25 ranked stale chrome/cache handoffs and cache-clear reports highest for the cache/widget/route query

Remaining friction shifted from manual file-list assembly to orchestration: a temporary caller still had to chain inventory, selection, file reads, regex, BM25, log, metadata, and handoff capture.

## What Should Stay Manual

Keep these decisions explicit for safety and clarity:

- choosing the investigation target
- deciding which evidence categories are relevant
- deciding whether adjacent checkouts such as `Y:/BlogAI` are in scope
- reviewing whether selected inputs may contain secrets before sending them to MCP tools
- choosing whether a finding deserves a durable handoff
- approving any future production cache-clear, app recycle, deployment, auth, or BlogEngine.NET change

## What Should Stay Explicit-Input Only

These boundaries should remain true for the MCP search tools:

- `bridge_regex_text_search` receives only `inputText` or `entries`
- `bridge_bm25_text_search` receives only `documents` or `entries`
- neither tool accepts paths as read instructions
- neither tool crawls repositories
- neither tool mutates state
- neither tool stores a persistent index
- both tools execute through `BridgeToolExecutor`

Any future helper may read files before calling MCP, but that helper must record exactly which files were read and what entries/documents were supplied.

## Black-Box Risks To Avoid

Avoid improvements that make results harder to audit:

- autonomous whole-repo crawling hidden behind a tool call
- opaque AI or embedding indexes with unclear source membership
- silent fallback from MCP to `rg`
- mutation mixed into search workflows
- automatic production endpoint calls during search
- hidden credential or token handling inside search inputs
- generated handoffs that omit request ids, operation ids, input manifests, or fallback status

## Conservative Future Slices

Possible next slices, in order of usefulness:

1. **Explicit repo document selection helper**
   - Status: first constrained MCP helper added as `bridge_select_repo_documents`.
   - Input: caller-supplied root-relative include/exclude patterns and optional category hints.
   - Output: deterministic selected file metadata.
   - Boundary: MCP search tools still receive explicit text only; the selector does not search content.

2. **Search workflow template**
   - Input: manifest, regex terms, BM25 queries.
   - Output: repeatable log, metadata, and handoff skeleton.
   - Boundary: no new MCP tools.

3. **Trace bundle inventory helper**
   - Input: run name.
   - Output: matching log, metadata, diagram, and handoff references.
   - Boundary: read-only repo artifact inventory.

4. **Canonical-vs-historical evidence tagging**
   - Input: small curated manifests for BlogAI evidence groups.
   - Output: explicit categories agents can filter before building entries.
   - Boundary: human-curated tags, not automatic authority inference.

5. **Caller-side BM25 snippet extraction**
   - Input: BM25 ranked document ids plus original explicit documents.
   - Output: short local snippets for review.
   - Boundary: deterministic snippet extraction, not model summarization.

## Recommended Next Step

The smallest useful next tooling slice is a repo-owned explicit-input MCP search workflow runner.

It should:

- read a caller-approved manifest
- build explicit entries/documents outside MCP
- call `bridge_get_tool_inventory`
- call `bridge_regex_text_search` and/or `bridge_bm25_text_search`
- emit log and metadata artifacts
- record request ids, operation ids, input files, labels, match summaries, ranked results, and fallback status

It should not:

- add MCP-side path reading
- add crawler behavior
- add mutation
- add persistent indexing
- change MCP transport
- call production BlogAI endpoints
