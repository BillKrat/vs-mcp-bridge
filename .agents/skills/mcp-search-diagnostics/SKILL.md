---
name: mcp-search-diagnostics
description: Use MCP-exposed bridge search diagnostics over explicit caller-provided text or document entries.
---

# MCP Search Diagnostics

## Use When

- Searching explicit text gathered during MCP/tooling triage.
- Checking exact markers, regex patterns, source snippets, docs, or preserved evidence without adding crawler behavior.
- Ranking a small caller-selected document set by relevance.
- Repeating a prior `rg` or manual search through the MCP tool path for platform validation.

## Tool Choice

- Start with `bridge_get_tool_inventory` to confirm compiled search tools are visible.
- Use `bridge_select_repo_documents` when you need deterministic repo-root-relative file metadata before assembling explicit search entries.
- Use `bridge_regex_text_search` for exact, regex, structural, or deterministic marker searches.
- Use `bridge_bm25_text_search` for ranked relevance searches over explicit documents or entries.
- Use deterministic repo search such as `rg` when MCP tool access is unavailable, and record that fallback path in the handoff.

## Workflow

1. Call `bridge_get_tool_inventory`.
2. Optionally call `bridge_select_repo_documents` with explicit include/exclude patterns to produce a visible file list.
3. Read only the selected files you choose in the caller.
4. Choose `bridge_regex_text_search` or `bridge_bm25_text_search`.
5. Build a bounded set of explicit `inputText`, `entries`, or `documents` in the caller.
6. Pass only that text into the MCP search tool.
7. Preserve selected files, matched entries, request/operation ids, and the search path in a handoff when the result affects future work.

## Safety Rules

- Do not pass filesystem paths and expect the MCP search tools to read them.
- `bridge_select_repo_documents` accepts repo-root-relative patterns and returns metadata only; it does not search file contents or call regex/BM25.
- Do not add implicit file crawling or repository scanning to the MCP search tools.
- Do not mutate state.
- Do not include secrets, tokens, passwords, bearer values, or private credentials in search inputs.
- Both search tools execute through `BridgeToolExecutor`; preserve its policy, approval, redaction, audit, manifest, and correlation boundaries.

## References

- `docs/tool-execution-trace-workflow.md`
- `docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md`
- `docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md`
- `docs/session-handoffs/2026-05-16-blogai-doc-selection-search-workflow.md`
