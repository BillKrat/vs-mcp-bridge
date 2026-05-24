# BlogAI MCP Search Workflow Findings

## Workflow Target

This session used both MCP-exposed search diagnostics against the BlogAI stale shared chrome/cache issue:

- `bridge_regex_text_search`
- `bridge_bm25_text_search`

The workflow was analysis/SolutionFolder/docs/artifacts only. It did not implement BlogAI features, auth, deployment changes, file crawling, mutation, or MCP transport changes.

The caller selected a bounded evidence set, read those files outside the MCP tool boundary, and passed only explicit text entries/documents into MCP.

## Inventory Result

Inventory path used:

- MCP `tools/list`
- MCP `bridge_get_tool_inventory`

Observed inventory:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `19`
- `tools/list` included `bridge_get_tool_inventory`: yes
- `tools/list` included `bridge_regex_text_search`: yes
- `tools/list` included `bridge_bm25_text_search`: yes
- `bridge_get_tool_inventory` succeeded
- compiled inventory count: `2`
- compiled inventory included `bridge.regexTextSearch`: yes
- compiled inventory included `bridge.bm25TextSearch`: yes

Inventory request id:

- `0cd85dae602f4282906f08f2c953d24a`

## Explicit Documents

The workflow used these explicit caller-built entries/documents:

| Index | Id | Description | Files |
| ---: | --- | --- | ---: |
| 0 | `stale-inspection` | Stale shared chrome inspection report | 1 |
| 1 | `cache-clear-failure` | Cache clear failure inspection report | 1 |
| 2 | `final-route-after-cache-clear` | Final rendered route verification after failed cache clear | 1 |
| 3 | `cache-reload-inspection` | BlogAI cache reload inspection report | 1 |
| 4 | `widget-before` | Preserved before-update widget settings row `26512` | 3 |
| 5 | `widget-after` | Current after-update widget settings row `26512` | 3 |
| 6 | `local-blogai-cache-source` | Selected local BlogAI cache/auth source files | 2 |
| 7 | `canonical-posts` | Canonical `SolutionFolder/docs/blogs/posts` aggregate | 32 |

Evidence log:

- `SolutionFolder/artifacts/logs/blogai-mcp-search-workflow-20260516.log`

## Regex Search Summary

Tool path used:

- `bridge_regex_text_search`
- fallback `rg`: no

Regex terms searched:

| Term | Total Matches | Matched Entries |
| --- | ---: | --- |
| `feature/approval-apply-ui-slice` | 49 | `stale-inspection`, `cache-clear-failure`, `final-route-after-cache-clear`, `widget-before` |
| `clearCache` | 13 | `stale-inspection`, `cache-clear-failure`, `final-route-after-cache-clear`, `local-blogai-cache-source` |
| `TextBox` | 11 | `stale-inspection`, `cache-clear-failure`, `canonical-posts` |
| `widget` | 81 | all entries |
| `route` | 51 | `stale-inspection`, `cache-clear-failure`, `final-route-after-cache-clear`, `cache-reload-inspection`, `canonical-posts` |
| `blogAi` | 71 | all entries |

Key regex findings:

- The stale branch marker still did not match canonical `SolutionFolder/docs/blogs/posts`.
- The stale branch marker still did not match selected local BlogAI cache/auth source files.
- The stale branch marker still did not match the current after-update widget row `26512` settings.
- The stale branch marker matched stale diagnostic reports, failed rendered-route evidence, and preserved before-update widget evidence.
- `clearCache` matched the local BlogAI settings API source and the cache-clear failure docs, confirming the relevant operational path is the admin settings cache reset route, not a post-body rewrite path.

## BM25 Search Summary

Tool path used:

- `bridge_bm25_text_search`
- fallback `rg`: no

BM25 queries:

| Query | Top Ranked Results |
| --- | --- |
| `cache clear endpoint shared widget chrome stale marker` | `cache-clear-failure`, `final-route-after-cache-clear`, `stale-inspection`, `canonical-posts`, `cache-reload-inspection` |
| `BlogEngine widget settings stale chrome deployment cache` | `final-route-after-cache-clear`, `cache-clear-failure`, `stale-inspection`, `canonical-posts`, `local-blogai-cache-source` |
| `BlogAI source of truth publishing route validation` | `canonical-posts`, `final-route-after-cache-clear`, `cache-reload-inspection`, `stale-inspection`, `cache-clear-failure` |

Key BM25 findings:

- Ranked search correctly surfaced the cache-clear failure report and final rendered-route failure report for cache/chrome queries.
- Ranked search correctly surfaced canonical posts first for the source-of-truth publishing route query.
- BM25 was useful for orienting among known evidence documents, while regex was better for proving exact marker absence/presence.

## Comparison To Previous Findings

This MCP-tool-based workflow matches the prior `rg` and manual findings:

- The stale marker evidence remains historical/preserved/rendered-failure evidence, not canonical post source.
- The likely issue remains cached shared widget/page chrome, not article body content.
- Current after-update widget settings and selected local source do not contain the stale marker.
- The operational path still points toward local/admin verification of BlogEngine cache-clear behavior before any deployment action.

The main difference is that this run used the platform path under test: MCP inventory, MCP regex search, and MCP BM25 search.

## Tooling Gaps

Observed gaps from real use:

- A temporary direct MCP stdio helper was still needed to run repeatable inventory/search workflows.
- Explicit document assembly is caller-owned. That is the intended safety boundary, but future sessions would benefit from a repo-owned manifest-driven helper that builds entries outside MCP and records what was supplied.
- Regex results are entry-level unless the caller splits entries per file. A helper should encourage file-per-entry inputs when file-level attribution matters.
- BM25 ranking returns scores and document ids, but no snippets or term-hit explanation. Snippets would make ranked results easier to review without reading full documents.
- Artifact capture is still manual. A small repo-owned runner could emit log and metadata consistently without adding MCP file crawling.

## Recommended Next MCP/Tooling Slice

Add a repo-owned MCP diagnostic runner for explicit-input workflows.

The runner should:

- accept a checked-in or caller-provided manifest of file paths
- read files outside MCP
- call `bridge_get_tool_inventory`, `bridge_regex_text_search`, and `bridge_bm25_text_search`
- emit a log and metadata artifact with request ids, operation ids, inputs, matches, ranked results, and fallback status

This should not add MCP-side crawling, path reading, mutation, or transport changes.

## Recommended Next BlogAI Operational Action

Keep the next BlogAI action local and operational:

- verify the real BlogEngine admin-authenticated `clearCache` path locally
- confirm whether `PUT /api/settings?action=clearCache` resets the widget cache path used by TextBox widgets
- preserve before/after evidence before considering any production cache-clear, app recycle, or deployment action

## Deferred Items

- BlogAI auth implementation
- OAuth/OpenID
- production deployment changes
- production publishing automation
- BlogEngine.NET migration
- MCP file crawling
- BM25 persistent indexing
- mutation tools
