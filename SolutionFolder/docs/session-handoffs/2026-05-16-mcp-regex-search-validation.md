# MCP Regex Search Validation

## Summary

`bridge.regexTextSearch` is now exposed through MCP as the diagnostic wrapper `bridge_regex_text_search`.

The MCP wrapper accepts only explicit caller-provided text input and executes the existing compiled bridge tool through `BridgeToolExecutor`.
It does not crawl files, read paths, mutate state, call ChatEngine, call the VSIX named pipe, change MCP transport architecture, or expose BM25.

## Checkpoint

- Branch: `main`
- Starting HEAD: `260a457 Capture BlogAI stale chrome search pressure test`
- Validation date: 2026-05-22

## Implementation

- MCP tool name: `bridge_regex_text_search`
- Bridge tool id: `bridge.regexTextSearch`
- Wrapper location: `VsMcpBridge.McpServer/Tools/VsTools.cs`
- Execution boundary: `IBridgeToolExecutor.ExecuteAsync`
- Inputs:
  - `pattern`
  - optional `inputText`
  - optional explicit `entries`
  - `caseSensitive`
  - optional `maxResults`

The wrapper builds a `BridgeToolRequest` with a fresh request id and operation id, then delegates to `BridgeToolExecutor`.
The executor still owns catalog lookup, policy, approval, secret-reference handling, redaction, audit, manifest metadata, and correlation.

## Validated Behavior

Unit validation:

- `dotnet test .\VsMcpBridge.Shared.Tests\VsMcpBridge.Shared.Tests.csproj`
- result: 257 passed
- `dotnet build .\VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj`
- result: build succeeded with 0 warnings and 0 errors
- `git diff --check`
- result: no whitespace errors; Git reported existing line-ending normalization warnings for edited C# files

Direct MCP validation:

- server: `VsMcpBridge.McpServer 1.0.0.0`
- `tools/list` count: `18`
- `tools/list` includes `bridge_regex_text_search`: yes
- successful regex call returned:
  - tool id: `bridge.regexTextSearch`
  - request id: `1aaa4f65369b494eb4e77bd7a3970029`
  - operation id: `c802106d919f4f95a09cf2578d8cb773`
  - match count: `1`
  - total match count: `2`
  - limited: `true`
- invalid regex call returned structured failure:
  - success: `false`
  - error code: `InvalidRegex`

Durable evidence:

- `SolutionFolder/artifacts/logs/mcp-regex-search-trace-20260516.log`
- `SolutionFolder/artifacts/logs/mcp-regex-search-trace-20260516.metadata.json`
- `SolutionFolder/docs/diagrams/mcp-regex-search-trace-20260516.mmd`

## Safety Boundaries

Preserved:

- no filesystem crawling
- no implicit repository scan
- no path access
- no mutation
- no VSIX behavior change
- no MCP transport architecture change
- no BM25 MCP wrapper
- no bypass around `BridgeToolExecutor`

## Remaining Friction

The direct MCP validation still used a temporary helper outside the repo.
A future slice can make this easier by adding a small repo-owned validation command for MCP-only diagnostics.

## Resume Guidance

For follow-up work, start with:

1. `AI_START.md`
2. this handoff
3. `SolutionFolder/docs/tool-execution-trace-workflow.md`
4. `SolutionFolder/docs/session-handoffs/2026-05-16-blogai-stale-chrome-search-findings.md`

Recommended next slice:

- use `bridge_regex_text_search` for a real BlogAI explicit-input search workload, then decide whether a BM25 MCP wrapper is justified.
