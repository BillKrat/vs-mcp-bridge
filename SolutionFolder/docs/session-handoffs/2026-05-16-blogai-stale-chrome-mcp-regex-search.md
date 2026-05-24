# BlogAI Stale Chrome MCP Regex Search

## Summary

This session repeated the prior BlogAI stale shared chrome/cache search workload using the MCP-exposed `bridge_regex_text_search` diagnostic instead of falling back to `rg`.

The result confirms the prior finding:

- `feature/approval-apply-ui-slice` was not found in canonical `SolutionFolder/docs/blogs/posts` material.
- It was not found in the selected local BlogAI source files for widget cache/settings behavior.
- It was not found in the current after-update widget row `26512` settings artifacts.
- Matches were limited to stale diagnostic reports, preserved before-update widget evidence, historical DB export sample rows, and the prior handoff that documented the same finding.

This proves the new MCP regex diagnostic closes the earlier pressure-test gap for explicit-input search workloads.

## Checkpoint

- Branch: `main`
- Starting HEAD: `da5a9d1 Expose regex text search through MCP`
- Capture date: 2026-05-22
- Work type: analysis/SolutionFolder/docs/artifacts only

## MCP Validation

Direct MCP stdio validation used a temporary caller outside the repository.

- Server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `18`
- `tools/list` included `bridge_regex_text_search`: yes
- Smoke invocation:
  - MCP tool: `bridge_regex_text_search`
  - bridge tool id: `bridge.regexTextSearch`
  - success: `true`
  - match count: `1`
- Invalid regex invocation:
  - success: `false`
  - error code: `InvalidRegex`

## Workload

Marker searched:

- `feature/approval-apply-ui-slice`

The caller built eight explicit text entries and passed them to `bridge_regex_text_search`.
The MCP tool was not given file paths and did not crawl the filesystem.

| Entry | Input | Files | Match Count |
| --- | --- | ---: | ---: |
| 0 | Canonical `SolutionFolder/docs/blogs/posts` aggregate | 32 | 0 |
| 1 | Local BlogAI selected source files | 2 | 0 |
| 2 | Current after-update widget settings row `26512` | 2 | 0 |
| 3 | Stale shared chrome inspection report | 1 | 11 |
| 4 | Final rendered route failure after cache clear report | 1 | 32 |
| 5 | Preserved before-update widget settings row `26512` | 3 | 5 |
| 6 | Historical DB export sample rows | 5 | 5 |
| 7 | Prior stale chrome MCP pressure-test handoff | 1 | 8 |

Overall result:

- request id: `85439ee022a74a1a98f9ac16a0d423e1`
- operation id: `deadbde2478547d3889330b1c24da8eb`
- match count: `61`
- total match count: `61`
- limited: `false`

## Evidence

- `SolutionFolder/artifacts/logs/blogai-stale-chrome-mcp-regex-search-20260516.log`
- `SolutionFolder/artifacts/logs/blogai-stale-chrome-mcp-regex-search-20260516.metadata.json`
- `SolutionFolder/docs/diagrams/blogai-stale-chrome-mcp-regex-search-20260516.mmd`

## Comparison To Prior Fallback Search

The prior pressure-test had to use deterministic `rg` fallback because `bridge.regexTextSearch` was inventory-visible but not MCP-callable.

This run used the actual MCP tool path:

1. direct MCP stdio caller listed tools
2. caller passed explicit text entries to `bridge_regex_text_search`
3. MCP wrapper executed `bridge.regexTextSearch` through `BridgeToolExecutor`
4. results preserved request and operation correlation metadata

The conclusion is unchanged, but the evidence now comes through the platform path being pressure-tested.

## Safety Boundaries

Preserved:

- no BlogAI auth implementation
- no production BlogEngine.NET modification
- no deployment change
- no MCP filesystem crawling
- no MCP path arguments
- no MCP transport change
- no BM25 MCP exposure
- no mutation

The temporary caller read selected files to construct explicit input text. That read happened outside the MCP tool boundary and is recorded in the metadata.

## Validation

- `git diff --check`
  - result: no whitespace errors; Git reported a line-ending normalization warning for `.gitignore`
- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
  - result: 257 passed
  - warnings: existing nullable/analyzer warnings in current code/tests
- `dotnet build ./VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj`
  - result: build succeeded with 0 warnings and 0 errors after clearing the temporary MCP server process

## Recommended Next Slice

Use `bridge_regex_text_search` for one small explicit-input BlogAI source/docs triage workflow during the first practical BlogAI implementation planning pass.

Do not expose BM25 until a real workflow needs ranked in-memory document search rather than deterministic regex matching.

## Follow-Up Note

The next MCP tooling slice exposed the compiled BM25 search capability through MCP as `bridge_bm25_text_search`.
That wrapper executes `bridge.bm25TextSearch` through `BridgeToolExecutor` and accepts only explicit `documents` or `entries` from the MCP request.
See `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-bm25-search-validation.md` and `SolutionFolder/artifacts/logs/mcp-bm25-search-trace-20260516.log`.
