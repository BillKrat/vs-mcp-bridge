---
name: mcp-validation
description: Validate MCP server startup, tool listing, chat ping, and pipe-backed tool activation boundaries.
---

# MCP Validation

## Use When

- Checking MCP stdio initialization or `tools/list`.
- Discovering available bridge tools for AI triage.
- Distinguishing MCP server failure from VSIX named-pipe activation failure.
- Validating `chat_engine_ping` or a pipe-backed MCP tool.
- Investigating hangs around MCP request flow.

## Workflow

1. Read `AI_START.md` and `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`.
2. Confirm whether the target tool is MCP-only or pipe-backed.
3. When MCP is reachable, call `bridge_get_tool_inventory` early to inspect deterministic manifest metadata for registered bridge tools. It is read-only, does not execute bridge tools, and currently reports compiled tools such as `bridge.bm25TextSearch` and `bridge.regexTextSearch`.
4. For search diagnostics, use `.agents/skills/mcp-search-diagnostics/SKILL.md`: choose `bridge_regex_text_search` for exact/regex matching and `bridge_bm25_text_search` for ranked relevance over explicit input text/documents.
5. For pipe-backed tools, ensure the Visual Studio Experimental Instance and `VS MCP Bridge` tool window are active.
6. Capture the first missing triage marker from the runbook rather than guessing from symptoms.
7. Keep MCP stdout protocol-clean; use StdErr, file logs, UI logs, or durable artifacts for diagnostics.
8. Record whether validation used direct MCP stdio, Visual Studio Copilot Agent, Cursor, or a helper script.

## References

- `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
- `.agents/skills/mcp-search-diagnostics/SKILL.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`
- `SolutionFolder/artifacts/logs/vsix-activation-diagnostic-trace-20260516.log`
- `SolutionFolder/docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`
