# MCP BM25 Search Validation

## Summary

`bridge.bm25TextSearch` is now exposed through MCP as the diagnostic wrapper `bridge_bm25_text_search`.

The MCP wrapper accepts only explicit caller-provided in-memory documents or entries and executes the existing compiled bridge tool through `BridgeToolExecutor`.
It does not crawl files, read paths, mutate state, call ChatEngine, call the VSIX named pipe, or change MCP transport architecture.

## Checkpoint

- Branch: `main`
- Starting HEAD: `29c713f Capture BlogAI stale chrome search with MCP regex tool`
- Validation date: 2026-05-22

## Implementation

- MCP tool name: `bridge_bm25_text_search`
- Bridge tool id: `bridge.bm25TextSearch`
- Wrapper location: `VsMcpBridge.McpServer/Tools/VsTools.cs`
- Execution boundary: `IBridgeToolExecutor.ExecuteAsync`
- Inputs:
  - `query`
  - optional explicit `documents`
  - optional explicit `entries`
  - `caseSensitive`
  - optional `maxResults`

The wrapper builds a `BridgeToolRequest` with a fresh request id and operation id, then delegates to `BridgeToolExecutor`.
The executor still owns catalog lookup, policy, approval, secret-reference handling, redaction, audit, manifest metadata, and correlation.

## Validated Behavior

Unit validation:

- `dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj`
- result: 262 passed
- `dotnet build ./VsMcpBridge.McpServer/VsMcpBridge.McpServer.csproj`
- result: build succeeded with 0 warnings and 0 errors
- `git diff --check`
- result: no whitespace errors; Git reported line-ending normalization warnings for edited files

Direct MCP validation:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `19`
- `tools/list` includes `bridge_bm25_text_search`: yes
- ranked BM25 call returned:
  - tool id: `bridge.bm25TextSearch`
  - request id: `b2f5a3c9174548f8bd692f8c9411f172`
  - operation id: `1c7b57acc47f4efd976617fce3b39a11`
  - result count: `2`
  - total result count: `3`
  - limited: `true`
  - top document index: `1`
- empty query call returned structured failure:
  - success: `false`
  - error code: `InvalidRequest`
- empty documents call returned structured failure:
  - success: `false`
  - error code: `InvalidRequest`

Durable evidence:

- `SolutionFolder/artifacts/logs/mcp-bm25-search-trace-20260516.log`
- `SolutionFolder/artifacts/logs/mcp-bm25-search-trace-20260516.metadata.json`
- `SolutionFolder/docs/diagrams/mcp-bm25-search-trace-20260516.mmd`

## Safety Boundaries

Preserved:

- no filesystem crawling
- no implicit repository scan
- no path access
- no mutation
- no VSIX behavior change
- no MCP transport architecture change
- no bypass around `BridgeToolExecutor`

## Resume Guidance

For follow-up work, start with:

1. `AI_START.md`
2. this handoff
3. `SolutionFolder/docs/tool-execution-trace-workflow.md`
4. `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-regex-search-validation.md`

Recommended next slice:

- use `bridge_bm25_text_search` only when a real explicit-input workflow benefits from ranked in-memory document search instead of deterministic regex matching.

Follow-up guidance:

- Future agents should use `.agents/skills/mcp-search-diagnostics/SKILL.md` before choosing between `bridge_regex_text_search`, `bridge_bm25_text_search`, and fallback `rg`.
