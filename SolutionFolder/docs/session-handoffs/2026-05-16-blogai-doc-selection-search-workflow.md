# BlogAI Document Selection Search Workflow

Evidence Classification: Operational Handoff
Intended Use: Resume context and findings from a BlogAI MCP search workflow.
Search Interpretation: Treat matches as workflow evidence and conclusions, not canonical blog content.
Currentness: Durable handoff for the 2026-05-22 MCP document-selection search run.

## Summary

This session repeated the BlogAI stale shared chrome/cache investigation using the full MCP diagnostic workflow:

1. `bridge_select_repo_documents`
2. `bridge_regex_text_search`
3. `bridge_bm25_text_search`
4. durable evidence artifacts

The workflow remained analysis/SolutionFolder/docs/artifacts only.
It did not implement BlogAI features, implement auth, change deployment, mutate files, add broad crawling, or change MCP transport.

## Checkpoint

- Branch: `main`
- Starting HEAD: `83a79ab Add explicit repo document selection helper`
- Capture date: 2026-05-22

## MCP Availability

Direct MCP stdio validation observed:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `20`
- `bridge_select_repo_documents`: available
- `bridge_regex_text_search`: available
- `bridge_bm25_text_search`: available
- `bridge_get_tool_inventory`: succeeded
- inventory request id: `c5c3bad2280949518320bcf73ed097d3`
- compiled inventory: `bridge.bm25TextSearch`, `bridge.regexTextSearch`

## Document Selection

Selection request id:

- `a967bf52775344298cd42dd57c33c53f`

Explicit include patterns:

- `SolutionFolder/docs/blogs/**/*.md`
- `SolutionFolder/docs/blogs/posts/**/*.html`
- `SolutionFolder/docs/blogs/posts/**/*.json`
- `SolutionFolder/docs/session-handoffs/*blogai*.md`
- `SolutionFolder/docs/session-handoffs/*BlogAI*.md`

Explicit exclude pattern:

- `SolutionFolder/docs/blogs/source-of-truth/db-export-20260516/**/*.json`

Selection result:

- candidate count: `90`
- selected count: `90`
- limited: `false`

Selected path summary:

| Category | Count |
| --- | ---: |
| `blog-docs` | 53 |
| `canonical-post-content` | 16 |
| `canonical-post-metadata` | 16 |
| `blogai-handoff` | 5 |

The helper materially reduced manual entry assembly: the caller no longer hand-built a file list from prior handoffs.
Instead, the caller supplied visible include/exclude patterns, reviewed deterministic selected metadata, then read those files outside MCP and supplied explicit file-per-entry text to regex/BM25.

## Regex Results

Search path:

- MCP `bridge_regex_text_search`
- fallback `rg`: no

| Term | Request Id | Operation Id | Matches | Matched Entries | Limited |
| --- | --- | --- | ---: | ---: | --- |
| `feature/approval-apply-ui-slice` | `18fea4c5bf234c7d885ff2d19e0659f9` | `29fcdfc81f5a4248a368fedbdf452482` | 127 | 28 | false |
| `clearCache` | `47697749de424c3ab712bfc3381a35d9` | `0d2cfa4e841645a289c4d9cd760b7072` | 22 | 7 | false |
| `TextBox` | `5c42dbf89b8b4d4cbe615889ca062108` | `4b264e1d0e1b4dbab4f60d4be69d4602` | 20 | 8 | false |
| `widget` | `22da636ee06a425d808150d8b00278d4` | `759553ffb03b42279077a15a0ea99937` | 115 | 12 | false |
| `route` | `f37ddb1decc3414e888938b5d1e4a4ec` | `91c9ffe8c7284e74adefa245a35a8b7d` | 238 | 36 | false |

Key exact-marker finding:

- `feature/approval-apply-ui-slice` matched durable reports, prepublish comparison/inspection evidence, and handoffs.
- It did not match selected canonical post content under `SolutionFolder/docs/blogs/posts/**/*.html`.
- It did not match selected canonical post metadata under `SolutionFolder/docs/blogs/posts/**/*.json`.

This preserves the prior conclusion: the stale marker evidence remains in historical/diagnostic/rendered-failure material, not in canonical post source.

## BM25 Results

Search path:

- MCP `bridge_bm25_text_search`
- fallback `rg`: no

Query:

- `BlogAI stale shared chrome cache widget route validation`

BM25 request:

- request id: `8e9d6930bc534c249b14c73d81546a9f`
- operation id: `f430b1b6ce66493e9e5f9d63dde630c8`
- result count: `10`
- total result count: `75`
- limited: `true`

Top ranked results:

1. `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-stale-chrome-search-findings.md`
2. `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-mcp-search-workflow-findings.md`
3. `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-pressure-test-findings.md`
4. `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-stale-chrome-mcp-regex-search.md`
5. `SolutionFolder/docs/blogs/blogengine-cache-clear-failure-inspection-20260516.md`
6. `SolutionFolder/docs/blogs/final-rendered-route-verification-after-cache-clear-20260516.md`
7. `SolutionFolder/docs/blogs/final-rendered-route-verification-20260516.md`
8. `SolutionFolder/docs/blogs/stale-shared-feature-branch-link-inspection-20260516.md`
9. `SolutionFolder/docs/blogs/README.md`
10. `SolutionFolder/docs/blogs/blogai-cache-reload-inspection-20260516.md`

The ranked results are consistent with the expected investigation target: prior stale chrome handoffs and cache-clear/rendered-route reports ranked ahead of canonical article content.

## Comparison To Prior Manual Workflow

Prior workflow:

- caller manually assembled eight aggregate entries
- aggregation reduced file-level attribution
- file membership had to be documented by hand

This workflow:

- selected 90 files through deterministic MCP metadata
- preserved file-per-entry attribution for regex and BM25
- avoided fallback `rg`
- kept search tools explicit-input only
- recorded selected categories and request/operation ids

The helper reduced manual selection friction without creating an opaque search/indexing layer.

## Remaining Ergonomics Gaps

- A temporary direct MCP stdio caller is still needed to orchestrate selector, regex, BM25, log, metadata, and handoff capture.
- Search workflow artifacts are still hand-authored after the run.
- Category hints are caller-provided and useful, but there is no curated BlogAI evidence manifest yet.
- BM25 still returns ranked ids/scores but no caller-side snippets.
- Selection metadata does not include authority labels such as canonical, historical, rendered-failure, or source-of-truth.

## Recommended Next MCP/Tooling Slice

Add a small explicit search workflow runner or template.

It should:

- call `bridge_get_tool_inventory`
- call `bridge_select_repo_documents`
- read selected files outside MCP search tools
- call regex and/or BM25 with explicit entries/documents
- emit log and metadata artifacts
- preserve fallback status

It should not add background indexing, implicit crawler behavior, mutation, production endpoint calls, or MCP transport changes.

## Recommended Next BlogAI Action

Continue treating the stale chrome issue as an operational cache/widget investigation:

- verify the BlogEngine admin-authenticated `clearCache` behavior locally
- confirm whether it resets the TextBox widget cache path
- preserve before/after evidence before considering production cache-clear, app recycle, or deployment action

## Evidence

- `SolutionFolder/artifacts/logs/blogai-doc-selection-search-workflow-20260516.log`
- `SolutionFolder/artifacts/logs/blogai-doc-selection-search-workflow-20260516.metadata.json`
- `SolutionFolder/docs/diagrams/blogai-doc-selection-search-workflow-20260516.mmd`

## Deferred

- BlogAI feature implementation
- authentication implementation
- OAuth/OpenID
- production deployment changes
- production cache-clear or app recycle
- BlogEngine.NET migration
- file mutation
- broad repo crawling
- hidden indexing
