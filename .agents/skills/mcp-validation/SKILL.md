---
name: mcp-validation
description: Validate MCP server startup, tool listing, chat ping, and pipe-backed tool activation boundaries.
---

# MCP Validation

## Use When

- Checking MCP stdio initialization or `tools/list`.
- Distinguishing MCP server failure from VSIX named-pipe activation failure.
- Validating `chat_engine_ping` or a pipe-backed MCP tool.
- Investigating hangs around MCP request flow.

## Workflow

1. Read `AI_START.md` and `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`.
2. Confirm whether the target tool is MCP-only or pipe-backed.
3. For pipe-backed tools, ensure the Visual Studio Experimental Instance and `VS MCP Bridge` tool window are active.
4. Capture the first missing triage marker from the runbook rather than guessing from symptoms.
5. Keep MCP stdout protocol-clean; use StdErr, file logs, UI logs, or durable artifacts for diagnostics.
6. Record whether validation used direct MCP stdio, Visual Studio Copilot Agent, Cursor, or a helper script.

## References

- `docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
- `docs/session-handoffs/2026-05-16-full-validation-checkpoint.md`
- `docs/session-handoffs/2026-05-16-vsix-activation-diagnostic-validation.md`
- `artifacts/logs/vsix-activation-diagnostic-trace-20260516.log`
- `docs/diagrams/vsix-activation-diagnostic-trace-20260516.mmd`
