# MCP Document Selection Validation

Evidence Classification: Diagnostic Trace
Intended Use: Validate the MCP document selection helper and preserve observed tool behavior.
Search Interpretation: Treat matches as platform validation evidence, not BlogAI source content.
Currentness: Durable validation handoff for the 2026-05-22 MCP diagnostic run.

## Summary

`bridge_select_repo_documents` is now exposed through MCP as a safe read-only diagnostic helper for explicit repo document selection.

The helper reduces manual file-list preparation before MCP regex/BM25 search workflows while preserving the explicit-input model.
It returns deterministic metadata only and does not search content, rank relevance, execute bridge tools, mutate files, build a hidden cache, or create an index.

## Checkpoint

- Branch: `main`
- Starting HEAD: `207e584 Document MCP search workflow ergonomics gaps`
- Validation date: 2026-05-22

## Implementation

- MCP tool name: `bridge_select_repo_documents`
- Location: `VsMcpBridge.McpServer/Tools/VsTools.cs`
- Inputs:
  - `includePatterns`
  - optional `excludePatterns`
  - optional `maxFiles`
  - optional `categoryHints`
- Output:
  - request id
  - selected count and limit flag
  - deterministic document list
  - relative path
  - source include pattern
  - optional category hint
  - size and line count metadata

The helper intentionally does not call `BridgeToolExecutor` because it does not execute bridge tools.
It is a metadata-selection diagnostic that makes the caller's eventual explicit search inputs visible before the caller reads file content and supplies `entries` or `documents` to regex/BM25.

## Validated Behavior

Unit validation:

- deterministic ordering
- include/exclude filtering
- `maxFiles` limiting
- `.git`, `.vs`, `bin`, `obj`, and `node_modules` path exclusion
- structured failure for broad whole-repo wildcard selection
- no file mutation
- no bridge tool executor call
- MCP allowlist includes `bridge_select_repo_documents`

Direct MCP validation:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `20`
- `tools/list` includes `bridge_select_repo_documents`: yes
- `tools/list` includes `bridge_regex_text_search`: yes
- `tools/list` includes `bridge_bm25_text_search`: yes
- selected candidate count: `10`
- returned document count: `8`
- limited: `true`
- request id: `840ad550e4c1471caca63d5450854f92`
- broad root wildcard call returned:
  - success: `false`
  - error code: `InvalidRequest`

Durable evidence:

- `artifacts/logs/mcp-document-selection-trace-20260516.log`
- `docs/diagrams/mcp-document-selection-trace-20260516.mmd`

## Safety Boundaries

Preserved:

- no arbitrary absolute path access
- no content search
- no relevance ranking
- no mutation
- no hidden background cache
- no automatic repo indexing
- no MCP search-tool filesystem crawling
- no VSIX behavior change
- no MCP transport architecture change

The selector can enumerate explicit pattern base directories, but broad whole-repo wildcard includes such as `**/*` are rejected.

## Recommended Next Use

Use this helper before a real MCP search workflow when manual document selection is the bottleneck:

1. call `bridge_get_tool_inventory`
2. call `bridge_select_repo_documents` with explicit include/exclude patterns
3. review the selected metadata list
4. read only the selected files needed for the workflow
5. pass explicit text entries/documents to `bridge_regex_text_search` or `bridge_bm25_text_search`
6. preserve selected files, request ids, results, and fallback status in a handoff when the result affects future work

## Deferred

- autonomous repo crawling
- persistent search session store
- trace bundle inventory helper
- canonical-vs-historical source tagging
- artifact group search helper
- BM25 snippet extraction
- production BlogAI changes
