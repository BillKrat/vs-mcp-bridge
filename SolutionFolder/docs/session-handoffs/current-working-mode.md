# Current Working Mode – VS MCP Bridge

## Primary UX Model (Authoritative)

- The VS MCP Bridge is now request-first.
- The main user interaction is a single prompt input at the bottom of the tool window.
- The UI behaves like a chat/request surface, not a validation harness.

## Legacy / Secondary Mode

- The detailed proposal editor and development controls still exist.
- These are considered development-only or secondary.
- They must not be treated as the primary user experience.
- Do not expand or optimize these surfaces unless explicitly requested.

## Proven Capabilities

- Codex-originated MCP calls work end-to-end.
- MCP Bridge server connects correctly via config.
- VSIX proposal flow works (proposal -> Keep -> apply).
- Experimental instance + tool window open requirement is known.
- Active document requires real editor focus.

## Known Gaps

- UI prompt input is NOT yet a true prompt-dispatch system.
- The Send button currently routes to proposal submission logic.
- Freeform prompts (e.g., "ping", "list projects") do not yet invoke tools.
- Tool-window log surface is not wired to production logging.

## Next Engineering Direction

- Implement a real prompt-dispatch path from the UI.
- Support non-edit tool calls first (active document, project list, error list).
- Return results into the activity stream.
- Do not redesign the MCP protocol or proposal engine.

## Do Not Regress To

- Proposal-editor-first UX
- Treating the tool window as a validation harness
- Assuming Send == proposal submission only
- Expanding developer-only controls as primary UX

## How To Use This File

- Every Codex task must treat this as the source of truth.
- Prompts should restate relevant parts of this file when needed.
- If the working model changes, this file must be updated first.
