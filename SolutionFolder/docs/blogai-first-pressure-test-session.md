# BlogAI First Pressure-Test Session

## Purpose

Run one practical BlogAI-oriented session against the current MCP/tool platform without implementing BlogAI features yet.

The session should prove whether the existing inventory, search, diagnostics, trace, and handoff practices are enough to start real BlogAI work, and it should record gaps found during actual use.

## Session Objectives

- Verify MCP tool inventory.
- Identify available bridge tools.
- Inspect BlogAI-related docs and source-of-truth material.
- Use regex and BM25 bridge tools where they fit the question.
- Capture platform or workflow gaps found during real usage.
- Create a durable handoff before ending the session.

## Startup Checklist

1. Read `AGENTS.md`.
2. Read `AI_START.md`.
3. Read `SolutionFolder/docs/blogai-functional-pressure-test-plan.md`.
4. Call `bridge_get_tool_inventory` when MCP is reachable.
5. Confirm compiled bridge tools are visible, especially `bridge.regexTextSearch` and `bridge.bm25TextSearch`.

## Allowed Work

This first session may include:

- analysis
- triage
- documentation
- small safe repo-local findings
- identifying missing tools or missing evidence

Do not implement BlogAI features unless a later slice explicitly approves that scope.

## Out Of Scope

- runtime code changes
- authentication implementation
- new projects or packages
- deployment changes
- BlogEngine.NET changes
- production publishing automation

## Expected Outputs

- A findings handoff under `SolutionFolder/docs/session-handoffs/` if the session produces decisions or resume context.
- A missing-tool list based on real BlogAI usage.
- Workflow trace candidates worth capturing in a later validation slice.
- A recommended next slice.

## Closeout Check

Before ending the session, record what was actually tried, which tools were available, which gaps were observed, and what should happen next.
