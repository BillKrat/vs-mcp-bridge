---
name: trace-artifact-workflow
description: Capture repeatable workflow evidence, correlated logs, metadata, and Mermaid diagrams from observed runs.
---

# Trace Artifact Workflow

## Use When

- A workflow matters for development, triage, or future AI sessions.
- Adding or validating an observable request path.
- Creating durable logs, metadata, and Mermaid sequence diagrams.
- Comparing observed runtime behavior with intended code flow.
- Capturing or refreshing MCP inventory diagnostic evidence for `bridge_get_tool_inventory`.
- Capturing MCP search diagnostic evidence for `bridge_regex_text_search` or `bridge_bm25_text_search`.

## Workflow

1. Start from the relevant workflow doc instead of inventing a new artifact shape.
2. Use deterministic request IDs, operation IDs, run names, and input summaries.
3. Capture observed logs and result metadata before writing the Mermaid diagram.
4. For MCP search diagnostics, record which explicit entries/documents were supplied and whether MCP tools or fallback `rg` were used.
5. Store dated artifacts rather than overwriting existing evidence.
6. Compare the generated sequence against current code and document any mismatch.
7. Add or update a session handoff only when the run changes the future resume point.

## References

- `SolutionFolder/docs/tool-execution-trace-workflow.md`
- `.agents/skills/mcp-search-diagnostics/SKILL.md`
- `SolutionFolder/docs/session-handoffs/2026-05-16-mcp-tool-inventory-live-validation.md`
- `SolutionFolder/docs/app-host-ping-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-ping-trace-workflow.md`
- `SolutionFolder/docs/vsix-host-selected-text-trace-workflow.md`
- `SolutionFolder/docs/LOGGING_DIAGNOSTIC_RUNBOOK.md`
- `SolutionFolder/artifacts/logs/`
- `SolutionFolder/docs/diagrams/`
- `SolutionFolder/docs/session-handoffs/`
